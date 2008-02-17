// mDCM: A C# DICOM library
//
// Copyright (c) 2006-2008  Colby Dillion
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//
// Author:
//    Colby Dillion (colby.dillion@gmail.com)

using System;
using System.Collections.Generic;
using System.IO;
using System.Net; 
using System.Net.Sockets;
using System.Text;
using System.Threading;

using Dicom.Network;

using NLog;

namespace Dicom.Network.Client {
	public delegate void DcmResponseCallback(byte presentationID, ushort messageID, DcmStatus status);

	public class DcmClientBase : DcmNetworkBase {
		#region Private Members
		private string _callingAe;
		private string _calledAe;
		private uint _maxPdu;
		private DcmPriority _priority;
		private ManualResetEvent _closedEvent;
		private bool _closedOnError;
		private string _error;
		#endregion

		#region Public Constructors
		public DcmClientBase() : base() {
			_maxPdu = 32768;
			_priority = DcmPriority.High;
			_closedOnError = false;
			_closedEvent = new ManualResetEvent(false);
			_error = "No error";
		}
		#endregion

		#region Public Properties
		public string CallingAE {
			get { return _callingAe; }
			set { _callingAe = value; }
		}

		public string CalledAE {
			get { return _calledAe; }
			set {
				_calledAe = value;
				LogID = _calledAe;
			}
		}

		public uint MaxPduSize {
			get { return _maxPdu; }
			set { _maxPdu = value; }
		}

		public bool ClosedOnError {
			get { return _closedOnError; }
		}

		public DcmPriority Priority {
			get { return _priority; }
			set { _priority = value; }
		}

		public string ErrorMessage {
			get { return _error; }
		}
		#endregion

		#region Public Methods
		public void Close() {
			InternalClose(true);
		}

		protected void InternalClose(bool fireClosedEvent) {
			ShutdownNetwork();
			if (_closedEvent != null && fireClosedEvent) {
				_closedEvent.Set();
				_closedEvent = null;
			}
		}

		public bool Wait() {
			_closedEvent.WaitOne();
			return !_closedOnError;
		}
		#endregion

		#region DcmNetworkBase Overrides
		protected override void OnConnectionClosed() {
			Close();
		}

		protected override void OnNetworkError(Exception e) {
			_error = e.Message;
			_closedOnError = true;
			Close();
		}

		protected override void OnDimseTimeout() {
			_closedOnError = true;
			Close();
		}

		protected override void OnReceiveAssociateReject(DcmRejectResult result, DcmRejectSource source, DcmRejectReason reason) {
			_closedOnError = true;
			Close();
		}

		protected override void OnReceiveAbort(DcmAbortSource source, DcmAbortReason reason) {
			_closedOnError = true;
			SendReleaseRequest();
		}

		protected override void OnReceiveReleaseResponse() {
			_closedOnError = false;
			Close();
		}

		protected override void OnReceiveReleaseRequest() {
			SendReleaseResponse();
		}
		#endregion
	}
}
