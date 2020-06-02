using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CodeFirstWebFramework;
using Newtonsoft.Json.Linq;

namespace RepairCafe {
	public class Database : CodeFirstWebFramework.Database {

		public Database(ServerConfig server) : base(server) {
		}

		/// <summary>
		/// A database version number stored in the Settings table. Used to check if any extra changes
		/// need to be made on version change.
		/// </summary>
		public override int CurrentDbVersion {
			get {
				return 1;
			}
		}

		/// <summary>
		/// Code that must be run after the database is reconfigured - e.g. populating new fields
		/// </summary>
		/// <param name="version">Original version (-1 = new database)</param>
		public override void PostUpgradeFromVersion(int version) {
			initialiseDatabase();
		}

		void initialiseDatabase() {
			if (QueryOne("SELECT idRepairCafe FROM RepairCafe") == null) {
				Update(new RepairCafe() {
					Name = "Oswestry & Borders",
					FormEntryFields = "Date,FormNumber,PreviousVisitor,Name,Email,CanContact,Postcode,Phone,ItemForRepair,ItemType,Brand,ModelNo,WhatIsWrong,Happy,Repaired,CustomerComments,Repairer,RepairerComments"
				});
			}
			if (QueryOne("SELECT idContactType FROM ContactType") == null) {
				foreach (string ct in new string[] { "Visitor", "Repairer", "Volunteer" }) {
					Update(new ContactType() { TypeOfContact = ct });
				}
			}
			if (QueryOne("SELECT idItemType FROM ItemType") == null) {
				int sequence = 1;
				foreach (string it in new string[] { "Mechanical", "Electrical", "Sewing", "Furniture", "Bicycle", "Other" }) {
					Update(new ItemType() { TypeOfItem = it, RepairCafeId = 1, Sequence = sequence++ });
				}
			}
			if (QueryOne("SELECT idRepairStatus FROM RepairStatus") == null) {
				foreach (string it in new string[] { "Unknown", "Yes", "Partial", "No" }) {
					Update(new RepairStatus() { Status = it });
				}
			}
		}

	}
}
