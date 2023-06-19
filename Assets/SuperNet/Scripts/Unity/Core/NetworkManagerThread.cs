using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace SuperNet.Unity.Core {

	/// <summary>
	/// Provides API access to static network methods.
	/// </summary>
	public partial class NetworkManager : MonoBehaviour {

		// Resources
		private static ConcurrentQueue<Action> UpdateQueue;
		private static Thread UpdateThread;

		private static void InitializeThread() {
			UpdateQueue = new ConcurrentQueue<Action>();
			UpdateThread = null;
		}

		private void OnUpdateThread() {
			while (UpdateQueue.TryDequeue(out Action action)) {
				try {
					action.Invoke();
				} catch (Exception exception) {
					if (LogExceptions) {
						Debug.LogException(exception);
					}
				}
			}
		}

		private void OnAwakeThread() {
			Interlocked.CompareExchange(ref UpdateThread, Thread.CurrentThread, null);
		}

		/// <summary>
		/// Get main unity thread.
		/// </summary>
		/// <returns>Main unity thread.</returns>
		public static Thread GetMainThread() {
			return UpdateThread;
		}

		/// <summary>
		/// Queue action to be ran on the main unity thread.
		/// </summary>
		/// <param name="action">Action to run.</param>
		public static void Run(Action action) {
			if (action == null) {
				throw new ArgumentNullException(nameof(action), "No action provided");
			} else if (Thread.CurrentThread == UpdateThread) {
				action.Invoke();
			} else {
				UpdateQueue.Enqueue(action);
			}
		}

		/// <summary>
		/// Queue a coroutine to be ran on the main unity thread.
		/// </summary>
		/// <param name="routine">Routine to run.</param>
		/// <returns>The created coroutine.</returns>
		public static Coroutine Run(IEnumerator routine) {
			if (routine == null) {
				throw new ArgumentNullException(nameof(routine), "No routine provided");
			} else {
				return GetInstance().StartCoroutine(routine);
			}
		}

		/// <summary>
		/// Queue action to be ran on the main unity thread after a delay.
		/// </summary>
		/// <param name="action">Action to run.</param>
		/// <param name="seconds">Delay in seconds.</param>
		public static void Run(Action action, float seconds) {
			if (action == null) {
				throw new ArgumentNullException(nameof(action), "No action provided");
			} else if (seconds < 0) {
				throw new ArgumentOutOfRangeException(nameof(seconds), "Delay is negative");
			} else if (Thread.CurrentThread == UpdateThread) {
				NetworkManager instance = GetInstance();
				instance.StartCoroutine(RunCoroutine(action, seconds));
			} else {
				UpdateQueue.Enqueue(() => GetInstance().StartCoroutine(RunCoroutine(action, seconds)));
			}
		}

		/// <summary>
		/// Queue action to be ran on a separate thread.
		/// </summary>
		/// <param name="action">Action to run.</param>
		public static void RunAsync(Action action) {
			if (action == null) {
				throw new ArgumentNullException(nameof(action), "No action provided");
			} else {
				RunTask(action, CancellationToken.None);
			}
		}

		/// <summary>
		/// Queue action to be ran on a separate thread.
		/// </summary>
		/// <param name="action">Action to run.</param>
		/// <param name="token">Cancellation token to use.</param>
		public static void RunAsync(Action action, CancellationToken token) {
			if (action == null) {
				throw new ArgumentNullException(nameof(action), "No action provided");
			} else {
				RunTask(action, token);
			}
		}

		/// <summary>
		/// Queue task to be ran on a separate thread after a delay.
		/// </summary>
		/// <param name="action">Task to run.</param>
		/// <param name="millisecondsDelay">Delay in milliseconds.</param>
		public static void RunAsync(Func<Task> action, int millisecondsDelay = 0) {
			if (action == null) {
				throw new ArgumentNullException(nameof(action), "No action provided");
			} else {
				RunTask(action, millisecondsDelay, CancellationToken.None);
			}
		}

		/// <summary>
		/// Queue task to be ran on a separate thread after a delay.
		/// </summary>
		/// <param name="action">Task to run.</param>
		/// <param name="token">Cancellation token to use.</param>
		/// <param name="millisecondsDelay">Delay in milliseconds.</param>
		public static void RunAsync(Func<Task> action, CancellationToken token, int millisecondsDelay = 0) {
			if (action == null) {
				throw new ArgumentNullException(nameof(action), "No action provided");
			} else {
				RunTask(action, millisecondsDelay, token);
			}
		}

		private static IEnumerator RunCoroutine(Action action, float seconds) {
			yield return new WaitForSeconds(seconds);
			try {
				action.Invoke();
			} catch (Exception exception) {
				if (LogExceptions) {
					Debug.LogException(exception);
				}
			}
		}

		private static void RunTask(Action action, CancellationToken token) {
			Task.Factory.StartNew(
				() => {
					try {
						if (Thread.CurrentThread == UpdateThread && LogExceptions) {
							Debug.LogWarning("[SuperNet] [Manager] Asynchronous action scheduled on main thread.");
						}
						action.Invoke();
					} catch (Exception exception) {
						if (LogExceptions) {
							Debug.LogException(exception);
						}
					}
				}, token, TaskCreationOptions.PreferFairness, TaskScheduler.Default
			).ConfigureAwait(false);
		}

		private static void RunTask(Func<Task> action, int millisecondsDelay, CancellationToken token) {
			Task.Factory.StartNew(
				async () => {
					try {
						if (Thread.CurrentThread == UpdateThread && LogExceptions) {
							Debug.LogWarning("[SuperNet] [Manager] Asynchronous task scheduled on main thread.");
						}
						if (millisecondsDelay > 0) await Task.Delay(millisecondsDelay, token);
						await action.Invoke();
					} catch (Exception exception) {
						if (LogExceptions) {
							Debug.LogException(exception);
						}
					}
				}, token, TaskCreationOptions.PreferFairness, TaskScheduler.Default
			).ConfigureAwait(false);
		}

	}

}
