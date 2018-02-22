using System;
using System.ComponentModel;
using System.Timers;

namespace WatchTower.iOS
{
	/// <summary>
	/// Background worker wrapper.  Class to use to simplify setting up background worker functions.
	/// 
	/// Constructor takes the methodToCallToDoWork and methodToCallWhenWorkCompleted arguments, which
	/// are called on Tick and workCompleted.
	/// </summary>
	public class BackgroundWorkerWrapper
	{

			BackgroundWorker _backgroundWorker;
			Timer _timer;


		DelegateDefinitions.DoWorkOrWorkCompletedDelegate _doWorkMethod;
		DelegateDefinitions.DoWorkOrWorkCompletedDelegate _workCompletedMethod;
		int _timingInterval;


		/// <summary>
		/// Initializes a new instance of the <see cref="T:WatchTower.iOS.BackgroundWorkerWrapper"/> class.
		/// </summary>
		/// <param name="methodToCallToDoWork">Method to call to do work.</param>
		/// <param name="methodToCallWhenWorkCompleted">Method to call when work completed.</param>
		/// <param name="interval">Interval, in milliseconds</param>
		public BackgroundWorkerWrapper(DelegateDefinitions.DoWorkOrWorkCompletedDelegate methodToCallToDoWork, 
		                               DelegateDefinitions.DoWorkOrWorkCompletedDelegate methodToCallWhenWorkCompleted, 
		                               int interval)
		{
			// attach the methods related to doing work
			_doWorkMethod = methodToCallToDoWork;
			_workCompletedMethod = methodToCallWhenWorkCompleted;
			_timingInterval = interval;

			// set up the background thread
			_backgroundWorker = new BackgroundWorker();
			_backgroundWorker.RunWorkerCompleted += BackgroundWorker_WorkCompleted;
			_backgroundWorker.DoWork += BackgroundWorker_DoWork;

			// initialize the timer
			_timer = new Timer();
			_timer.Elapsed += OnTick;
		}


		/// <summary>
		/// Call this method to start the work associated with this worker.  Work will continue at specified interval until
		/// StopWork() is called.
		/// </summary>
		/// <param name="interval">Interval.</param>
		public void StartWork(int interval)
		{
			_timer.Stop();
			_timingInterval = interval;
			_timer.Interval = _timingInterval;
			_timer.Start();
		}


		/// <summary>
		/// Stops the work.  No more work is done until StartWork() is called.
		/// </summary>
		public void StopWork()
		{
			_timer.Stop();
		}


		/// <summary>
		/// Sets the timer interval used to start work.
		/// </summary>
		/// <param name="interval">Interval.</param>
		public void SetInterval(int interval)
		{
			_timingInterval = interval;
		}


		/// <summary>
		/// This method is called when the associated timer elapses.
		/// </summary>
		/// <param name="source">Source.</param>
		/// <param name="e">E.</param>
		private void OnTick(object source, ElapsedEventArgs e)
		{
			_timer.Stop();
			_backgroundWorker.RunWorkerAsync();
		}


		/// <summary>
		/// Called when the work associated with this object is completed.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		private void BackgroundWorker_WorkCompleted(object sender, RunWorkerCompletedEventArgs e)
		{

			_workCompletedMethod();
			_timer.Interval = _timingInterval;
			_timer.Start();
		}


		/// <summary>
		/// The method assigned to the DoWork event handler of the associated background worker
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			_doWorkMethod();
		}

	}
}
