using CodeFirstWebFramework;
using System;
using System.Collections.Generic;
using System.Text;

namespace RepairCafe {
	public abstract class AppModule : CodeFirstWebFramework.AppModule {
		public Session RCSession { get { return (Session)base.Session; } }
		public int Cafe { get { return RCSession == null || RCSession.RepairCafe == null ? 0 : RCSession.RepairCafe.idRepairCafe.GetValueOrDefault(); } }

		protected bool GetCurrentCafe() {
			Session session = (Session)RCSession;
			if (session.RepairCafe == null) {
				Redirect($"/home/repaircafes?{FromHere}");
				return false;
			}
			Title += " - " + session.RepairCafe.Name;
			return true;
		}

		public JObjectEnumerable SelectItemType() {
			return Database.Query($"SELECT idItemType AS id, TypeOfItem AS value FROM ItemType WHERE RepairCafeId = {Cafe} ORDER BY Sequence");
		}

	}

	public class Session : CodeFirstWebFramework.WebServer.Session {
		//
		// Summary:
		//     Empty constructor for when read from database
		public Session() : base() {
		}
		//
		// Summary:
		//     Constructor
		//
		// Parameters:
		//   server:
		public Session(WebServer server) : base(server) {
		}

		public RepairCafe RepairCafe;

	}
}
