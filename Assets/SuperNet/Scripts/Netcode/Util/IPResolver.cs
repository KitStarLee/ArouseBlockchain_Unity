using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable IDE0090 // Use 'new(...)'

namespace SuperNet.Netcode.Util {

	/// <summary>
	/// Helper methods that convert a connection string to an <c>IPEndPoint</c> used by the netcode.
	/// </summary>
	public static class IPResolver {

		/// <summary>
		/// Perform an asynchronous DNS lookup if needed and create an <c>IPEndPoint</c>.
		/// <para>
		/// Host must be a valid hostname without a port
		/// such as <c>192.168.12.43</c> or <c>superversus.com</c>.
		/// </para>
		/// </summary>
		/// <param name="host">Hostname to resolve.</param>
		/// <param name="port">Port to use.</param>
		/// <returns>A valid <c>IPEndPoint</c> with the provided IP address and port.</returns>
		public static async Task<IPEndPoint> ResolveAsync(string host, int port) {

			// Validate and try to parse
			host = host?.Trim();
			if (string.IsNullOrEmpty(host)) {
				throw new ArgumentNullException(nameof(host), "Host is null");
			} else if (port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort) {
				throw new ArgumentException(string.Format("Bad port {0}", port), nameof(port));
			} else if (IPAddress.TryParse(host, out IPAddress address)) {
				return new IPEndPoint(address, port);
			}

			// DNS
			IPAddress[] ips = await Dns.GetHostAddressesAsync(host);
			if (ips == null || ips.Length != 1) {
				throw new ArgumentException(string.Format("Multiple hosts in {0}", host), nameof(host));
			} else {
				return new IPEndPoint(ips[0], port);
			}

		}

		/// <summary>
		/// Perform an asynchronous DNS lookup if needed and create an <c>IPEndPoint</c>.
		/// <para>
		/// Host must be a valid hostname without a port
		/// such as <c>192.168.12.43</c> or <c>superversus.com</c>.
		/// </para>
		/// </summary>
		/// <param name="host">Hostname to resolve.</param>
		/// <param name="port">Port to use.</param>
		/// <param name="token">Cancellation token that can stop the DNS lookup before it is completed.</param>
		/// <returns>A valid <c>IPEndPoint</c> with the provided IP address and port.</returns>
		public static async Task<IPEndPoint> ResolveAsync(string host, int port, CancellationToken token) {

			// Validate and try to parse
			host = host?.Trim();
			if (string.IsNullOrEmpty(host)) {
				throw new ArgumentNullException(nameof(host), "Host is null");
			} else if (port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort) {
				throw new ArgumentException(string.Format("Bad port {0}", port), nameof(port));
			} else if (IPAddress.TryParse(host, out IPAddress address)) {
				return new IPEndPoint(address, port);
			}

			// DNS
			IPAddress[] ips = await GetHostAddressesAsync(host, token);
			if (ips == null || ips.Length != 1) {
				throw new ArgumentException(string.Format("Multiple hosts in {0}", host), nameof(host));
			} else {
				return new IPEndPoint(ips[0], port);
			}

		}

		/// <summary>
		/// Perform an asynchronous DNS lookup if needed and create an <c>IPEndPoint</c>.
		/// <para>
		/// Host must be a valid hostname, followed by a colon and a port
		/// such as <c>192.168.12.43:80</c> or <c>superversus.com:44015</c>.
		/// </para>
		/// </summary>
		/// <param name="host">Hostname with port to resolve.</param>
		/// <returns>A valid <c>IPEndPoint</c> with the provided IP address and port.</returns>
		public static async Task<IPEndPoint> ResolveAsync(string host) {

			// Validate and try to parse
			host = host?.Trim();
			int hostColon = host == null ? -1 : host.LastIndexOf(':'), port;
			string hostPrefix = hostColon >= 0 ? host?.Substring(0, hostColon) : host;
			string hostSuffix = hostColon >= 0 ? host?.Substring(hostColon + 1) : "";
			if (string.IsNullOrEmpty(host)) {
				throw new ArgumentNullException(nameof(host), "Host is null");
			} else if (string.IsNullOrEmpty(hostSuffix) || string.IsNullOrEmpty(hostPrefix)) {
				throw new ArgumentException(string.Format("No port in {0}", host), nameof(host));
			} else if (!int.TryParse(hostSuffix, out port) || port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort) {
				throw new ArgumentException(string.Format("Bad port {0} in {1}", hostSuffix, host), nameof(host));
			} else if (IPAddress.TryParse(hostPrefix, out IPAddress address)) {
				return new IPEndPoint(address, port);
			}

			// DNS
			IPAddress[] ips = await Dns.GetHostAddressesAsync(hostPrefix);
			if (ips == null || ips.Length != 1) {
				throw new ArgumentException(string.Format("Multiple hosts {0} in {1}", hostPrefix, host), nameof(host));
			} else {
				return new IPEndPoint(ips[0], port);
			}

		}

		/// <summary>
		/// Perform an asynchronous DNS lookup if needed and create an <c>IPEndPoint</c>.
		/// <para>
		/// Host must be a valid hostname, followed by a colon and a port
		/// such as <c>192.168.12.43:80</c> or <c>superversus.com:44015</c>.
		/// </para>
		/// </summary>
		/// <param name="host">Hostname with port to resolve.</param>
		/// <param name="token">Cancellation token that can stop the DNS lookup before it is completed.</param>
		/// <returns>A valid <c>IPEndPoint</c> with the provided IP address and port.</returns>
		public static async Task<IPEndPoint> ResolveAsync(string host, CancellationToken token) {

			// Validate and try to parse
			host = host?.Trim();
			int hostColon = host == null ? -1 : host.LastIndexOf(':'), port;
			string hostPrefix = hostColon >= 0 ? host?.Substring(0, hostColon) : host;
			string hostSuffix = hostColon >= 0 ? host?.Substring(hostColon + 1) : "";
			if (string.IsNullOrEmpty(host)) {
				throw new ArgumentNullException(nameof(host), "Host is null");
			} else if (string.IsNullOrEmpty(hostSuffix) || string.IsNullOrEmpty(hostPrefix)) {
				throw new ArgumentException(string.Format("No port in {0}", host), nameof(host));
			} else if (!int.TryParse(hostSuffix, out port) || port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort) {
				throw new ArgumentException(string.Format("Bad port {0} in {1}", hostSuffix, host), nameof(host));
			} else if (IPAddress.TryParse(hostPrefix, out IPAddress address)) {
				return new IPEndPoint(address, port);
			}

			// DNS
			IPAddress[] ips = await GetHostAddressesAsync(hostPrefix, token);
			if (ips == null || ips.Length != 1) {
				throw new ArgumentException(string.Format("Multiple hosts {0} in {1}", hostPrefix, host), nameof(host));
			} else {
				return new IPEndPoint(ips[0], port);
			}

		}

		/// <summary>
		/// Perform an asynchronous DNS lookup if needed and create an <c>IPEndPoint</c>.
		/// All exceptions are thrown via the callback.
		/// <para>
		/// Host must be a valid hostname without a port
		/// such as <c>192.168.12.43</c> or <c>superversus.com</c>.
		/// </para>
		/// </summary>
		/// <param name="host">Hostname to resolve.</param>
		/// <param name="port">Port to use.</param>
		/// <param name="callback">Callback to invoke after DNS lookup completes.</param>
		public static void Resolve(string host, int port, Action<IPEndPoint, Exception> callback) {
			try {

				// Validate and try to parse
				host = host?.Trim();
				if (string.IsNullOrEmpty(host)) {
					throw new ArgumentNullException(nameof(host), "Host is null");
				} else if (port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort) {
					throw new ArgumentException(string.Format("Bad port {0}", port), nameof(port));
				} else if (IPAddress.TryParse(host, out IPAddress address)) {
					callback.BeginInvoke(new IPEndPoint(address, port), null, a => callback.EndInvoke(a), null);
					return;
				}

				// DNS
				Dns.BeginGetHostAddresses(host, new AsyncCallback(result => {
					try {
						IPAddress[] ips = Dns.EndGetHostAddresses(result);
						if (ips == null || ips.Length != 1) {
							throw new ArgumentException(string.Format(
								"Multiple hosts in {0}", host
							), nameof(host));
						} else {
							callback.Invoke(new IPEndPoint(ips[0], port), null);
						}
					} catch (Exception e) {
						callback.Invoke(null, e);
					}
				}), null);

			} catch (Exception e) {
				callback.BeginInvoke(null, e, a => callback.EndInvoke(a), null);
			}
		}


		/// <summary>
		/// Perform an asynchronous DNS lookup if needed and create an <c>IPEndPoint</c>.
		/// All exceptions are thrown via the callback.
		/// <para>
		/// Host must be a valid IP address, followed by a colon and a port
		/// such as <c>192.168.12.43:80</c> or <c>127.0.0.1:44015</c>.
		/// </para>
		/// </summary>
		/// <param name="host">Hostname with port to resolve.</param>
		/// <param name="callback">Callback to invoke after DNS lookup completes.</param>
		public static void Resolve(string host, Action<IPEndPoint, Exception> callback) {
			try {

				// Validate and try to parse
				host = host?.Trim();
				int hostColon = host == null ? -1 : host.LastIndexOf(':'), port = -1;
				string hostPrefix = hostColon >= 0 ? host?.Substring(0, hostColon) : host;
				string hostSuffix = hostColon >= 0 ? host?.Substring(hostColon + 1) : "";
				if (string.IsNullOrEmpty(host)) {
					throw new ArgumentNullException(nameof(host), "Host is null");
				} else if (string.IsNullOrEmpty(hostSuffix) || string.IsNullOrEmpty(hostPrefix)) {
					throw new ArgumentException(string.Format("No port in {0}", host), nameof(host));
				} else if (!int.TryParse(hostSuffix, out port) || port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort) {
					throw new ArgumentException(string.Format("Bad port {0} in {1}", hostSuffix, host), nameof(host));
				} else if (IPAddress.TryParse(hostPrefix, out IPAddress address)) {
					callback.BeginInvoke(new IPEndPoint(address, port), null, a => callback.EndInvoke(a), null);
					return;
				}

				// DNS
				Dns.BeginGetHostAddresses(hostPrefix, new AsyncCallback(result => {
					try {
						IPAddress[] ips = Dns.EndGetHostAddresses(result);
						if (ips == null || ips.Length != 1) {
							throw new ArgumentException(string.Format(
								"Multiple hosts {0} in {1}", hostPrefix, host
							), nameof(host));
						} else {
							callback.Invoke(new IPEndPoint(ips[0], port), null);
						}
					} catch (Exception e) {
						callback.Invoke(null, e);
					}
				}), null);

			} catch (Exception e) {
				callback.BeginInvoke(null, e, a => callback.EndInvoke(a), null);
			}
			
		}

		/// <summary>
		/// Perform a synchronous DNS lookup if needed and create an <c>IPEndPoint</c>.
		/// <para>
		/// Host must be a valid hostname without a port
		/// such as <c>192.168.12.43</c> or <c>superversus.com</c>.
		/// </para>
		/// </summary>
		/// <param name="host">Hostname to resolve.</param>
		/// <param name="port">Port to use.</param>
		/// <returns>Resolved address.</returns>
		public static IPEndPoint Resolve(string host, int port) {

			// Validate and try to parse
			host = host?.Trim();
			if (string.IsNullOrEmpty(host)) {
				throw new ArgumentNullException(nameof(host), "Host is null");
			} else if (port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort) {
				throw new ArgumentException(string.Format("Bad port {0}", port), nameof(port));
			} else if (IPAddress.TryParse(host, out IPAddress address)) {
				return new IPEndPoint(address, port);
			}

			// DNS
			IPAddress[] ips = Dns.GetHostAddresses(host);
			if (ips == null || ips.Length != 1) {
				throw new ArgumentException(string.Format(
					"Multiple hosts in {0}", host
				), nameof(host));
			} else {
				return new IPEndPoint(ips[0], port);
			}

		}

		/// <summary>
		/// Perform a synchronous DNS lookup if needed and create an <c>IPEndPoint</c>.
		/// <para>
		/// Host must be a valid IP address, followed by a colon and a port
		/// such as <c>192.168.12.43:80</c> or <c>127.0.0.1:44015</c>.
		/// </para>
		/// </summary>
		/// <param name="host">Hostname with port to resolve.</param>
		/// <returns>Resolved address.</returns>
		public static IPEndPoint Resolve(string host) {

			// Validate and try to parse
			host = host?.Trim();
			int hostColon = host == null ? -1 : host.LastIndexOf(':'), port;
			string hostPrefix = hostColon >= 0 ? host?.Substring(0, hostColon) : host;
			string hostSuffix = hostColon >= 0 ? host?.Substring(hostColon + 1) : "";
			if (string.IsNullOrEmpty(host)) {
				throw new ArgumentNullException(nameof(host), "Host is null");
			} else if (string.IsNullOrEmpty(hostSuffix) || string.IsNullOrEmpty(hostPrefix)) {
				throw new ArgumentException(string.Format("No port in {0}", host), nameof(host));
			} else if (!int.TryParse(hostSuffix, out port) || port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort) {
				throw new ArgumentException(string.Format("Bad port {0} in {1}", hostSuffix, host), nameof(host));
			} else if (IPAddress.TryParse(hostPrefix, out IPAddress address)) {
				return new IPEndPoint(address, port);
			}

			// DNS
			IPAddress[] ips = Dns.GetHostAddresses(hostPrefix);
			if (ips == null || ips.Length != 1) {
				throw new ArgumentException(string.Format(
					"Multiple hosts {0} in {1}", hostPrefix, host
				), nameof(host));
			} else {
				return new IPEndPoint(ips[0], port);
			}

		}

		/// <summary>
		/// Try to parse the host as an IP address.
		/// This method never throws any exceptions and returns immediately.
		/// <para>
		/// Host must contain a valid IP address
		/// such as <c>192.168.12.43</c> or <c>127.0.0.1</c>.
		/// </para>
		/// </summary>
		/// <param name="host">IP address to parse.</param>
		/// <param name="port">Port to use.</param>
		/// <returns>Parsed <c>IPEndPoint</c> or null if invalid.</returns>
		public static IPEndPoint TryParse(string host, int port) {
			host = host?.Trim();
			if (string.IsNullOrEmpty(host) || port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort) {
				return null;
			} else if (IPAddress.TryParse(host, out IPAddress ip)) {
				return new IPEndPoint(ip, port);
			} else {
				return null;
			}
		}

		/// <summary>
		/// Try to parse the host as an IP address followed by a colon and a part.
		/// This method never throws any exceptions and returns immediately.
		/// <para>
		/// Host must be a valid IP address, followed by a colon and a port
		/// such as <c>192.168.12.43:80</c> or <c>127.0.0.1:44015</c>.
		/// </para>
		/// </summary>
		/// <param name="host">IP address with port to parse.</param>
		/// <returns>Parsed <c>IPEndPoint</c> or null if invalid.</returns>
		public static IPEndPoint TryParse(string host) {
			host = host?.Trim();
			int hostColon = host == null ? -1 : host.LastIndexOf(':');
			string hostPrefix = hostColon >= 0 ? host?.Substring(0, hostColon) : host;
			string hostSuffix = hostColon >= 0 ? host?.Substring(hostColon + 1) : "";
			if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(hostSuffix) || string.IsNullOrEmpty(hostPrefix)) {
				return null;
			} else if (!int.TryParse(hostSuffix, out int port) || port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort) {
				return null;
			} else if (IPAddress.TryParse(hostPrefix, out IPAddress address)) {
				return new IPEndPoint(address, port);
			} else {
				return null;
			}
		}

		/// <summary>
		/// Get local IPv4 address other machines on the same network can use to connect to us.
		/// This can be used to create LAN connections.
		/// </summary>
		/// <param name="port">Port to use.</param>
		/// <returns>Local IPv4 address or 127.0.0.1 if none found.</returns>
		public static IPEndPoint GetLocalAddress(int port) {
			return new IPEndPoint(GetLocalAddress(), port);
		}

		/// <summary>
		/// Get local IPv6 address other machines on the same network can use to connect to us.
		/// This can be used to create LAN connections.
		/// </summary>
		/// <param name="port">Port to use.</param>
		/// <returns>Local IPv6 address or ::1 if none found.</returns>
		public static IPEndPoint GetLocalAddressIPv6(int port) {
			return new IPEndPoint(GetLocalAddressIPv6(), port);
		}

		/// <summary>
		/// Get local IPv4 address other machines on the same network can use to connect to us.
		/// This can be used to create LAN connections.
		/// </summary>
		/// <returns>Local IPv4 address or 127.0.0.1 if none found.</returns>
		public static IPAddress GetLocalAddress() {
			string host = Dns.GetHostName();
			IPAddress[] ips = Dns.GetHostAddresses(host);
			foreach (IPAddress ip in ips) {
				if (IPAddress.IsLoopback(ip)) continue;
				if (ip.AddressFamily != AddressFamily.InterNetwork) continue;
				return ip;
			}
			return IPAddress.Loopback;
		}

		/// <summary>
		/// Get local IPv6 address other machines on the same network can use to connect to us.
		/// This can be used to create LAN connections.
		/// </summary>
		/// <returns>Local IPv6 address or ::1 if none found.</returns>
		public static IPAddress GetLocalAddressIPv6() {
			string host = Dns.GetHostName();
			IPAddress[] ips = Dns.GetHostAddresses(host);
			foreach (IPAddress ip in ips) {
				if (IPAddress.IsLoopback(ip)) continue;
				if (ip.AddressFamily != AddressFamily.InterNetworkV6) continue;
				return ip;
			}
			return GetLocalAddress().MapToIPv6();
		}

		/// <summary>DNS lookup that can be cancelled by a token.</summary>
		private static async Task<IPAddress[]> GetHostAddressesAsync(string host, CancellationToken token) {
			TaskCompletionSource<IPAddress[]> tcs = new TaskCompletionSource<IPAddress[]>();
			token.Register(() => tcs.TrySetCanceled());
			Dns.BeginGetHostAddresses(host, result => {
				try {
					tcs.TrySetResult(Dns.EndGetHostAddresses(result));
				} catch (Exception e) {
					tcs.TrySetException(e);
				}
			}, null);
			return await tcs.Task;
		}

	}

}
