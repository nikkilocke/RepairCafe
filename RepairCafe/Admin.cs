using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CodeFirstWebFramework;
using Newtonsoft.Json.Linq;

namespace RepairCafe {
	[Auth(AccessLevel.Admin)]               // All methods only accessable to Admins, except where overridden below
	[Implementation(typeof(AdminHelper))]   // This pulls in the default behaviour from CodeFirstWebFramework, which we can override
	public class Admin : AppModule {
		/// <summary>
		/// Add menu options
		/// </summary>
		protected override void Init() {
			base.Init();
			InsertMenuOptions(
				new MenuOption("Repair Cafes", "/admin/repaircafes"),
				new MenuOption("Contact Types", "/admin/contacttypes"),
				new MenuOption("Batch Jobs", "/admin/batchjobs"),
				new MenuOption("Backup", "/admin/backup"),
				new MenuOption("Restore", "/admin/restore"),
				new MenuOption("Settings", "/admin/settings")
				);
			if (SecurityOn) {
				if (RCSession.User != null) {
					InsertMenuOption(new MenuOption("My Settings", "/admin/usersettings"));
					InsertMenuOption(new MenuOption("Change my password", "/admin/changepassword"));
				}
				InsertMenuOption(new MenuOption(RCSession.User == null ? "Login" : "Logout", RCSession.User == null ? "/admin/login" : "/admin/logout"));
			}
		}

		public DataTableForm RepairCafes() {
			InsertMenuOption(new MenuOption("New Repair Cafe", "/admin/editrepaircafe?id = 0"));
			return new DataTableForm(this, typeof(RepairCafe), false, "Name") { Select = "/admin/editrepaircafe?id=0" };
		}

		public JObjectEnumerable RepairCafesListing() {
			return Database.Query("SELECT * FROM RepairCafe ORDER BY Name");
		}

		public Form EditRepairCafe(int id) {
			Form form = new Form(this, typeof(RepairCafe));
			form.Data = Database.Get<RepairCafe>(id);
			// TODO canDelete
			return form;
		}

		public AjaxReturn EditRepairCafeSave(RepairCafe json) {
			Database.BeginTransaction();
			RepairCafe original = null;
			if (json.idRepairCafe > 0)
				Utils.Check(Database.TryGet((int)json.idRepairCafe, out original), "Repair Cafe not found");
			AjaxReturn r = SaveRecord(json);
			if(r.error == null) {
				RCSession.RepairCafe = json;
				string[] types = json.ItemTypes.Split(',', StringSplitOptions.RemoveEmptyEntries);
				if(original == null || original.ItemTypes != json.ItemTypes) {
					ItemType [] oldTypes = Database.Query<ItemType>($"SELECT * FROM ItemType WHERE RepairCafeId = {json.idRepairCafe} ORDER BY SEQUENCE").ToArray();

					for(int i = 0; i < oldTypes.Length; i++) {
						ItemType t = oldTypes[i];
						if (i >= types.Length) {
							Database.Delete(t);
							continue;
						}
						if (t.TypeOfItem == types[i])
							continue;
						t.TypeOfItem = "," + i;
						Database.Update(t);
					}
					for (int i = 0; i < types.Length; i++) {
						ItemType t = i >= oldTypes.Length ? new ItemType() {
							Sequence = i + 1,
							RepairCafeId = Cafe,
							TypeOfItem = types[i]
						} : oldTypes[i];
						t.Sequence = i + 1;
						t.RepairCafeId = Cafe;
						t.TypeOfItem = types[i];
						Database.Update(t);
					}
				}
			}
			return r;
		}

		public AjaxReturn EditRepairCafeDelete(int id) {
			Database.BeginTransaction();
			Database.Execute($"DELETE FROM ItemType WHERE RepairCafeId = {id}");
			AjaxReturn r = DeleteRecord("RepairCafe", id);
			if (r.error == null)
				Database.Commit();
			return r;
		}

		public DataTableForm ContactTypes() {
			InsertMenuOption(new MenuOption("New Repair Cafe", "/admin/editContactType?id = 0"));
			return new DataTableForm(this, typeof(ContactType)) { Select = "/admin/editContactType" };
		}

		public JObjectEnumerable ContactTypesListing() {
			return Database.Query("SELECT * FROM ContactType ORDER BY TypeOfContact");
		}

		public Form EditContactType(int id) {
			Form form = new Form(this, typeof(ContactType));
			form.Data = Database.Get<ContactType>(id);
			form.CanDelete = Database.QueryOne($"SELECT Type FROM Contact WHERE Type = {id}") == null;
			return form;
		}

		public AjaxReturn EditContactTypeSave(ContactType json) {
			return SaveRecord(json);
		}

		public AjaxReturn EditContactTypeDelete(int id) {
			return DeleteRecord("ContactType", id);
		}


	}
}
