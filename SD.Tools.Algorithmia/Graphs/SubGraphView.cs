﻿//////////////////////////////////////////////////////////////////////
// Algorithmia is (c) 2009 Solutions Design. All rights reserved.
// http://www.sd.nl
//////////////////////////////////////////////////////////////////////
// COPYRIGHTS:
// Copyright (c) 2009 Solutions Design. All rights reserved.
// 
// The Algorithmia library sourcecode and its accompanying tools, tests and support code
// are released under the following license: (BSD2)
// ----------------------------------------------------------------------
// Redistribution and use in source and binary forms, with or without modification, 
// are permitted provided that the following conditions are met: 
//
// 1) Redistributions of source code must retain the above copyright notice, this list of 
//    conditions and the following disclaimer. 
// 2) Redistributions in binary form must reproduce the above copyright notice, this list of 
//    conditions and the following disclaimer in the documentation and/or other materials 
//    provided with the distribution. 
// 
// THIS SOFTWARE IS PROVIDED BY SOLUTIONS DESIGN ``AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES, 
// INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A 
// PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL SOLUTIONS DESIGN OR CONTRIBUTORS BE LIABLE FOR 
// ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT 
// NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR 
// BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, 
// STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE 
// USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE. 
//
// The views and conclusions contained in the software and documentation are those of the authors 
// and should not be interpreted as representing official policies, either expressed or implied, 
// of Solutions Design. 
//
//////////////////////////////////////////////////////////////////////
// Contributers to the code:
//		- Frans  Bouma [FB]
//////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SD.Tools.Algorithmia.UtilityClasses;
using SD.Tools.BCLExtensions.SystemRelated;
using SD.Tools.Algorithmia.GeneralInterfaces;

namespace SD.Tools.Algorithmia.Graphs
{
	/// <summary>
	/// Class which represents a subgraph view on a main graph with a subset of the vertices/edges of the main graph.
	/// </summary>
	/// <typeparam name="TVertex">The type of the vertices in this graph.</typeparam>
	/// <typeparam name="TEdge">The type of the edges in the graph</typeparam>
	/// <remarks>SubGraphView instances are used to 'view' a subset of a bigger graph and maintain themselves based on the actions on the
	/// main graph. Adding/removing vertices / edges from this SubGraphView removes them only from this view, not from the main graph. Adding
	/// vertices/edges to the main graph will add the vertex/edge to this view if the added element meets criteria (implemented through polymorphism, 
	/// by default no criteria are set, so no vertex/edge is added if it's added to the main graph). Removing a vertex/edge from the main graph will remove
	/// the vertex / edge from this view if it's part of this view. As this view binds to events on the main graph, it's key to call Dispose() on an 
	/// instance of SubGraphView if it's no longer needed to make sure event handlers are cleaned up.
	/// This view has no adjacency lists, as they're located in the main graph. 
	/// </remarks>
	public class SubGraphView<TVertex, TEdge> : IDisposable, INotifyAsRemoved
		where TEdge : class, IEdge<TVertex>
	{
		#region Class Property Declarations
		private bool _isDisposed;
		private readonly HashSet<TVertex> _vertices;
		private readonly HashSet<TEdge> _edges;
		#endregion

		#region Events
		/// <summary>
		/// Event which is raised when a vertex has been added to this SubGraphView
		/// </summary>
		public event EventHandler<GraphChangeEventArgs<TVertex>> VertexAdded;
		/// <summary>
		/// Event which is raised when a vertex has been removed from this SubGraphView
		/// </summary>
		public event EventHandler<GraphChangeEventArgs<TVertex>> VertexRemoved;
		/// <summary>
		/// Event which is raised when an edge has been added to this SubGraphView
		/// </summary>
		public event EventHandler<GraphChangeEventArgs<TEdge>> EdgeAdded;
		/// <summary>
		/// Event which is raised when an edge has been removed from this SubGraphView
		/// </summary>
		public event EventHandler<GraphChangeEventArgs<TEdge>> EdgeRemoved;
		/// <summary>
		/// Event which is raised when the subgraphview is made empty. Observers can use this event to dispose an empty subgraphview to avoid dangling event handlers.
		/// </summary>
		public event EventHandler IsEmpty;
		/// <summary>
		/// Event which is raised when this instance was disposed.
		/// </summary>
		public event EventHandler Disposed;
		/// <summary>
		/// Raised when the implementing element has been removed from its container
		/// </summary>
		public event EventHandler HasBeenRemoved;
		#endregion


		/// <summary>
		/// Initializes a new instance of the <see cref="SubGraphView&lt;TVertex, TEdge&gt;"/> class.
		/// </summary>
		/// <param name="mainGraph">The main graph this SubGraphView is a view on.</param>
		public SubGraphView(GraphBase<TVertex, TEdge> mainGraph)
		{
			ArgumentVerifier.CantBeNull(mainGraph, "mainGraph");
			this.MainGraph = mainGraph;
			BindEvents();
			_vertices = new HashSet<TVertex>();
			_edges = new HashSet<TEdge>();
		}


		/// <summary>
		/// Adds the specified vertex.
		/// </summary>
		/// <param name="vertex">The vertex.</param>
		public void Add(TVertex vertex)
		{
			if(this.MainGraph.Contains(vertex))
			{
				_vertices.Add(vertex);
				OnVertexAdded(vertex);
			}
		}


		/// <summary>
		/// Adds the specified edge.
		/// </summary>
		/// <param name="edge">The edge.</param>
		public void Add(TEdge edge)
		{
			if(this.MainGraph.Contains(edge))
			{
				_edges.Add(edge);
				OnEdgeAdded(edge);
			}
		}


		/// <summary>
		/// Removes the vertex.
		/// </summary>
		/// <param name="toRemove">To remove.</param>
		/// <remarks>toRemove can't be null, as a graph can't have null vertices</remarks>
		public void Remove(TVertex toRemove)
		{
			ArgumentVerifier.CantBeNull(toRemove, "toRemove");
			_vertices.Remove(toRemove);
			OnVertexRemoved(toRemove);
			CheckIsEmpty();
		}


		/// <summary>
		/// Removes the edge.
		/// </summary>
		/// <param name="toRemove">To remove.</param>
		/// <remarks>toRemove can't be null as a graph can't have null edges</remarks>
		public void Remove(TEdge toRemove)
		{
			ArgumentVerifier.CantBeNull(toRemove, "toRemove");
			_edges.Remove(toRemove);
			OnEdgeRemoved(toRemove);
			CheckIsEmpty();
		}


		/// <summary>
		/// Determines whether this SubGraphView contains the passed in vertex.
		/// </summary>
		/// <param name="vertex">The vertex.</param>
		/// <returns>true if the vertex is in this SubGraphView, false otherwise. 
		/// </returns>
		public bool Contains(TVertex vertex)
		{
			ArgumentVerifier.CantBeNull(vertex, "vertex");
			return _vertices.Contains(vertex);
		}


		/// <summary>
		/// Determines whether this SubGraphView contains the passed in edge.
		/// </summary>
		/// <param name="edge">The edge.</param>
		/// <returns>
		/// true if the edge is in this SubGraphView, false otherwise.
		/// </returns>
		public bool Contains(TEdge edge)
		{
			ArgumentVerifier.CantBeNull(edge, "edge");
			return _edges.Contains(edge);
		}


		/// <summary>
		/// Marks this instance as removed. It raises ElementRemoved
		/// </summary>
		public void MarkAsRemoved()
		{
			this.HasBeenRemoved.RaiseEvent(this);
		}


		/// <summary>
		/// Called when a vertex has been added to this view
		/// </summary>
		/// <param name="vertex">The vertex.</param>
		protected virtual void OnVertexAdded(TVertex vertex)
		{
			this.VertexAdded.RaiseEvent(this, new GraphChangeEventArgs<TVertex>(vertex));
		}


		/// <summary>
		/// Called when an edge has been added to this view
		/// </summary>
		/// <param name="edge">The edge.</param>
		protected virtual void OnEdgeAdded(TEdge edge)
		{
			this.EdgeAdded.RaiseEvent(this, new GraphChangeEventArgs<TEdge>(edge));
		}


		/// <summary>
		/// Called when a vertex has been removed from this view
		/// </summary>
		/// <param name="vertex">The vertex.</param>
		protected virtual void OnVertexRemoved(TVertex vertex)
		{
			this.VertexRemoved.RaiseEvent(this, new GraphChangeEventArgs<TVertex>(vertex));
		}


		/// <summary>
		/// Called when an edge has been removed from this view
		/// </summary>
		/// <param name="edge">The edge.</param>
		protected virtual void OnEdgeRemoved(TEdge edge)
		{
			this.EdgeRemoved.RaiseEvent(this, new GraphChangeEventArgs<TEdge>(edge));
		}

		
		/// <summary>
		/// Handles the event that a new vertex was added to the main graph.
		/// </summary>
		/// <param name="vertexAdded">The vertex added.</param>
		/// <remarks>By default, this routine does nothing. If you want to add this vertex to this SubGraphView, you've to add it by calling Add
		/// in a derived class, overriding this method.</remarks>
		protected virtual void HandleVertexAddedToMainGraph(TVertex vertexAdded)
		{
			// nop
		}


		/// <summary>
		/// Handles the event that a new edge was added to the main graph
		/// </summary>
		/// <param name="edgeAdded">The edge added.</param>
		/// <remarks>By default, this routine does nothing. If you want to add this edge to this SubGraphView, you've to add it by calling Add
		/// in a derived class, overriding this method.</remarks>
		protected virtual void HandleEdgeAddedToMainGraph(TEdge edgeAdded)
		{
			// nop
		}


		/// <summary>
		/// Handles the event that an edge was removed from the main graph.
		/// </summary>
		/// <param name="edgeRemoved">The edge removed.</param>
		/// <remarks>The view automatically updates its own datastructures already, so use this method to perform additional work</remarks>
		protected virtual void HandleEdgeRemovedFromMainGraph(TEdge edgeRemoved)
		{
			// nop
		}

		
		/// <summary>
		/// Handles the event that a vertex was removed from the main graph.
		/// </summary>
		/// <param name="vertexRemoved">The vertex removed.</param>
		/// <remarks>The view automatically updates its own datastructures already, so use this method to perform additional work</remarks>
		protected virtual void HandleVertexRemovedFromMainGraph(TVertex vertexRemoved)
		{
			// nop
		}


		/// <summary>
		/// Binds the events.
		/// </summary>
		private void BindEvents()
		{
			this.MainGraph.EdgeAdded += new EventHandler<GraphChangeEventArgs<TEdge>>(MainGraph_EdgeAdded);
			this.MainGraph.EdgeRemoved += new EventHandler<GraphChangeEventArgs<TEdge>>(MainGraph_EdgeRemoved);
			this.MainGraph.VertexAdded += new EventHandler<GraphChangeEventArgs<TVertex>>(MainGraph_VertexAdded);
			this.MainGraph.VertexRemoved += new EventHandler<GraphChangeEventArgs<TVertex>>(MainGraph_VertexRemoved);
		}
		

		/// <summary>
		/// Unbinds the events.
		/// </summary>
		private void UnbindEvents()
		{
			this.MainGraph.EdgeAdded -= new EventHandler<GraphChangeEventArgs<TEdge>>(MainGraph_EdgeAdded);
			this.MainGraph.EdgeRemoved -= new EventHandler<GraphChangeEventArgs<TEdge>>(MainGraph_EdgeRemoved);
			this.MainGraph.VertexAdded -= new EventHandler<GraphChangeEventArgs<TVertex>>(MainGraph_VertexAdded);
			this.MainGraph.VertexRemoved -= new EventHandler<GraphChangeEventArgs<TVertex>>(MainGraph_VertexRemoved);
		}


		/// <summary>
		/// Checks if the subgraphview is empty and if so, it raises IsEmpty
		/// </summary>
		private void CheckIsEmpty()
		{
			if((_edges.Count <= 0) && (_vertices.Count <= 0))
			{
				this.IsEmpty.RaiseEvent(this);
			}
		}


		/// <summary>
		/// Handles the VertexRemoved event of the MainGraph control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The event arguments instance containing the event data.</param>
		private void MainGraph_VertexRemoved(object sender, GraphChangeEventArgs<TVertex> e)
		{
			Remove(e.InvolvedElement);
			HandleVertexRemovedFromMainGraph(e.InvolvedElement);
		}


		/// <summary>
		/// Handles the VertexAdded event of the MainGraph control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The event arguments instance containing the event data.</param>
		private void MainGraph_VertexAdded(object sender, GraphChangeEventArgs<TVertex> e)
		{
			HandleVertexAddedToMainGraph(e.InvolvedElement);
		}


		/// <summary>
		/// Handles the EdgeRemoved event of the MainGraph control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The event arguments instance containing the event data.</param>
		private void MainGraph_EdgeRemoved(object sender, GraphChangeEventArgs<TEdge> e)
		{
			Remove(e.InvolvedElement);
			HandleEdgeRemovedFromMainGraph(e.InvolvedElement);
		}


		/// <summary>
		/// Handles the EdgeAdded event of the MainGraph control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The event arguments instance containing the event data.</param>
		private void MainGraph_EdgeAdded(object sender, GraphChangeEventArgs<TEdge> e)
		{
			HandleEdgeAddedToMainGraph(e.InvolvedElement);
		}

		
		#region IDisposable Members
		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Releases unmanaged and - optionally - managed resources
		/// </summary>
		/// <param name="disposing"><see langword="true"/> to release both managed and unmanaged resources; <see langword="false"/> to release only unmanaged resources.</param>
		private void Dispose(bool disposing)
		{
			if(disposing && !_isDisposed)
			{
				UnbindEvents();
				_isDisposed = true;
				this.Disposed.RaiseEvent(this);
			}
		}
		#endregion

		#region Class Property Declarations
		/// <summary>
		/// Gets the main graph this SubGraphView is a view on
		/// </summary>
		public GraphBase<TVertex, TEdge> MainGraph { get; private set; }
		/// <summary>
		/// Gets the vertices contained in this SubGraphView. All vertices are part of this.MainGraph
		/// </summary>
		public IEnumerable<TVertex> Vertices 
		{
			get { return _vertices; }
		}

		/// <summary>
		/// Gets the edges contained in this SubGraphView. All edges are part of this.MainGraph
		/// </summary>
		public IEnumerable<TEdge> Edges 
		{
			get { return _edges; }
		}
		#endregion

	}
}