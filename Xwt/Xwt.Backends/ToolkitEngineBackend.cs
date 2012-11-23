// 
// EngineBackend.cs
//  
// Author:
//       Lluis Sanchez <lluis@xamarin.com>
//       Eric Maupin <ermau@xamarin.com>
// 
// Copyright (c) 2011-2012 Xamarin Inc
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Reflection;
using System.IO;
using Xwt.Drawing;
using Xwt.Engine;
using System.Collections.Generic;

namespace Xwt.Backends
{
	public abstract class ToolkitEngineBackend
	{
		Dictionary<Type,Type> backendTypes;
		Dictionary<Type,BackendHandler> sharedBackends = new Dictionary<Type, BackendHandler> ();
		ToolkitEngine toolkit;

		internal void Initialize (ToolkitEngine toolkit)
		{
			this.toolkit = toolkit;
			if (backendTypes == null) {
				backendTypes = new Dictionary<Type, Type> ();
				InitializeBackends ();
			}
			InitializeApplication ();
		}

		/// <summary>
		/// Initializes the application.
		/// </summary>
		public virtual void InitializeApplication ()
		{
		}

		/// <summary>
		/// Initializes the widget registry used by the application.
		/// </summary>
		/// <remarks>
		/// Don't do any toolkit initialization there, do them in InitializeApplication.
		/// Override to register the backend classes, by calling RegisterBackend() methods.
		/// </remarks>
		public virtual void InitializeBackends ()
		{
		}

		/// <summary>
		/// Runs the main GUI loop
		/// </summary>
		public abstract void RunApplication ();
		
		/// <summary>
		/// Exits the main GUI loop
		/// </summary>
		public abstract void ExitApplication ();
		
		/// <summary>
		/// Asynchronously invokes <paramref name="action"/> on the engine UI thread.
		/// </summary>
		/// <param name="action">The action to invoke.</param>
		/// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c>.</exception>
		public abstract void InvokeAsync (Action action);

		/// <summary>
		/// Begins invoking <paramref name="action"/> on a timer period of <paramref name="timeSpan"/>.
		/// </summary>
		/// <param name="action">The function to invoke. Returning <c>false</c> stops the timer.</param>
		/// <param name="timeSpan">The period before the initial invoke and between subsequent invokes.</param>
		/// <returns>An identifying object that can be used to cancel the timer with <seealso cref="CancelTimerInvoke"/>.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c>.</exception>
		/// <seealso cref="CancelTimerInvoke"/>
		public abstract object TimerInvoke (Func<bool> action, TimeSpan timeSpan);

		/// <summary>
		/// Cancels an invoke timer started from <see cref="TimerInvoke"/>.
		/// </summary>
		/// <param name="id">The unique object returned from <see cref="TimerInvoke"/>.</param>
		/// <exception cref="ArgumentNullException"><paramref name="id"/> is <c>null</c>.</exception>
		public abstract void CancelTimerInvoke (object id);
		
		/// <summary>
		/// Gets a reference to the native widget wrapped by an XWT widget
		/// </summary>
		/// <returns>
		/// The native widget.
		/// </returns>
		/// <param name='w'>
		/// A widget
		/// </param>
		public abstract object GetNativeWidget (Widget w);
		
		/// <summary>
		/// Gets a reference to the image object wrapped by an XWT Image
		/// </summary>
		/// <returns>
		/// The native image.
		/// </returns>
		/// <param name='image'>
		/// An image.
		/// </param>
		public virtual object GetNativeImage (Image image)
		{
			return ToolkitEngine.GetBackend (image);
		}

		public virtual void ShowWebBrowser (NavigateToUrlEventArgs e)
		{
			if (!e.Handled && e.Uri != null) {
				System.Diagnostics.Process.Start (e.Uri.ToString ());
				e.SetHandled ();
			}
		}

		/// <summary>
		/// Dispatches pending events in the UI event queue
		/// </summary>
		public abstract void DispatchPendingEvents ();
		
		/// <summary>
		/// Gets the backend for a native window.
		/// </summary>
		/// <returns>
		/// The backend for the window.
		/// </returns>
		/// <param name='nativeWindow'>
		/// A native window reference.
		/// </param>
		public abstract IWindowFrameBackend GetBackendForWindow (object nativeWindow);
		
		/// <summary>
		/// Gets the native parent window of a widget
		/// </summary>
		/// <returns>
		/// The native parent window.
		/// </returns>
		/// <param name='w'>
		/// A widget
		/// </param>
		/// <remarks>
		/// This method is used by XWT to get the window of a widget, when the widget is
		/// embedded in a native application
		/// </remarks>
		public virtual object GetNativeParentWindow (Widget w)
		{
			return null;
		}
		
		/// <summary>
		/// Gets a value indicating whether this <see cref="Xwt.Backends.EngineBackend"/> handles size negotiation on its own
		/// </summary>
		/// <value>
		/// <c>true</c> if the engine backend handles size negotiation; otherwise, <c>false</c>.
		/// </value>
		public virtual bool HandlesSizeNegotiation {
			get { return false; }
		}

		void CheckInitialized ()
		{
			if (backendTypes == null)
				throw new InvalidOperationException ("XWT toolkit not initialized");
		}

		internal T CreateBackend<T> (Type widgetType)
		{
			CheckInitialized ();
			Type bt = null;

			if (!backendTypes.TryGetValue (widgetType, out bt))
				return default(T);
			object res = Activator.CreateInstance (bt);
			if (!typeof(T).IsInstanceOfType (res))
				throw new InvalidOperationException ("Invalid backend type.");
			return (T) res;
		}

		internal T CreateSharedBackend<T> (Type widgetType) where T:BackendHandler
		{
			CheckInitialized ();
			BackendHandler res;
			if (!sharedBackends.TryGetValue (widgetType, out res)) {
				res = sharedBackends [widgetType] = CreateBackend<T> (widgetType);
				res.Initialize (toolkit);
			}
			return (T)res;
		}
		
		public void RegisterBackend (Type widgetType, Type backendType)
		{
			CheckInitialized ();
			backendTypes [widgetType] = backendType;
		}
		
		public T CreateFrontend<T> (object backend)
		{
			return (T) Activator.CreateInstance (typeof(T), backend);
		}

		/// <summary>
		/// Registers a callback to be invoked just before the execution returns to the main loop
		/// </summary>
		/// <param name='action'>
		/// Callback to execute
		/// </param>
		/// <remarks>
		/// The default implementation does the invocation using InvokeAsync.
		/// </remarks>			
		public virtual void InvokeBeforeMainLoop (Action action)
		{
			InvokeAsync (action);
		}
	}
}

