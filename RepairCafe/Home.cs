using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CodeFirstWebFramework;
using Newtonsoft.Json.Linq;

namespace RepairCafe {
	public class Home : AppModule {
		/// <summary>
		/// Add menu options
		/// </summary>
		protected override void Init() {
			base.Init();
			if (Cafe > 0)
				InsertMenuOptions(
					new MenuOption("Repair Forms", "/home/forms"),
					new MenuOption("Add Repair Forms", "/home/editform?id=0"),
					new MenuOption("Export Forms", "/home/export"),
					new MenuOption("Contacts", "/home/contacts")
					);
		}

		public override void Default() {
			if(GetCurrentCafe())
				Redirect("/home/forms");
		}

		public DataTableForm RepairCafes() {
			return new DataTableForm(this, typeof(RepairCafe), false, "Name") { Select = "/home/forms" };
		}

		public JObjectEnumerable RepairCafesListing() {
			return Database.Query("SELECT * FROM RepairCafe ORDER BY Name");
		}

		public DataTableForm Forms() {
			string c = GetParameters["id"];
			if (c == null) {
				if (!GetCurrentCafe())
					return null;
			} else {
				Utils.Check(Database.TryGet(int.Parse(c), out RepairCafe cafe), "Repair Cafe not found");
				RCSession.RepairCafe = cafe;
				GetCurrentCafe();
			}
			DataTableForm form = new DataTableForm(this, typeof(RepairForm), false, "Date", "FormNumber", "Name", "Email", "ItemType", "ItemForRepair", "Repairer");
			form.Select = "/home/editform";
			return form;
		}

		public JObjectEnumerable FormsListing() {
			return Database.Query($"SELECT * FROM RepairForm WHERE RepairCafe = {Cafe} ORDER BY Date DESC, FormNumber");
		}

		RepairForm lastForm() {
			return Database.QueryOne<RepairForm>($"SELECT * FROM RepairForm WHERE RepairCafe = {Cafe} ORDER BY idRepairForm DESC");
		}

		public Form EditForm(int id) {
			if (!GetCurrentCafe())
				return null;
			Form form = new Form(this, typeof(RepairForm), true, RCSession.RepairCafe.FormEntryFields.Split(','));
			makeContactAutoComplete(form["Name"], "Name", ContactType.Visitor, "Email", "Phone", "Postcode", "CanContact");
			makeContactAutoComplete(form["Email"], "Email", ContactType.Visitor);
			makeContactAutoComplete(form["Repairer"], "Email", ContactType.Repairer);
			makeRadio(form["ItemType"], SelectItemType());
			makeRadio(form["Repaired"]);
			RepairForm f;
			if (id > 0) {
				Utils.Check(Database.TryGet(id, out f), "Repair Form not found");
				form.CanDelete = true;
			} else {
				f = new RepairForm();
				RepairForm previous = lastForm();
				f.Date = previous.Date;
				f.FormNumber = previous.FormNumber + 1;
			}
			form.Data = f;
			form.Options["saveAndNew"] = true;
			return form;
		}

		void makeContactAutoComplete(FieldAttribute f, string field, int type, params string[] extras) {
			if (f == null)
				return;
			string moreFields = string.Join(", ", extras);
			if (!string.IsNullOrEmpty(moreFields))
				moreFields = ", " + moreFields;
			f.MakeSelectable(Database.Query($"SELECT {field} AS value{moreFields} FROM Contact WHERE RepairCafe = {Cafe} AND Type = {type} ORDER BY {field}"));
			f.Type = "autoComplete";
			f.Options["matchBeginning"] = true;
			f.Options["autoFill"] = true;
		}

		void makeRadio(FieldAttribute f, JObjectEnumerable values = null) {
			if (f == null)
				return;
			if (values != null)
				f.MakeSelectable(values);
			f.Type = "radioInput";
		}

		public AjaxReturn EditFormSave(RepairForm json) {
			Database.BeginTransaction();
			AjaxReturn r = SaveRecord(json);
			if(r.error == null) {
				checkContact(ContactType.Visitor, json.Name, json);
				checkContact(ContactType.Repairer, json.Repairer);
				Database.Commit();
			}
			return r;
		}

		public AjaxReturn EditFormDelete(int id) {
			return DeleteRecord("RepairForm", id);
		}

		void checkContact(int type, string name, RepairForm form = null) {
			if (string.IsNullOrEmpty(name))
				return;
			Contact c = Database.QueryOne<Contact>($@"SELECT * 
FROM Contact 
WHERE Type = {type}
AND RepairCafe = {Cafe}
AND Name = {Database.Quote(name)}
{(form == null || string.IsNullOrEmpty(form.Email) ? "" : ("AND Email = " + Database.Quote(form.Email)))}");
			if(c.idContact == null) {
				c.Name = name;
				c.FullName = name;
				c.Type = type;
				if (form != null) {
					c.Email = form.Email;
					c.Postcode = form.Postcode;
					c.Phone = form.Phone;
				}
				Database.Update(c);
			}
		}

		public enum ExportType {
			TabDelimited,
			CSV
		}

		[Writeable]
		public class ExportForm : JsonObject {
			public DateTime Date;
			public int ExportType;
			public bool Headings = true;
		}
		public DumbForm Export() {
			if (!GetCurrentCafe())
				return null;
			DumbForm form = new DumbForm(this, typeof(ExportForm));
			form["ExportType"].MakeSelectable(typeof(ExportType));
			form.Data = new ExportForm() { Date = lastForm().Date };
			return form;
		}

		public string tabQuote(string text) {
			return text.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
		}

		public string csvQuote(string text) {
			return Regex.IsMatch(text, "[\"\r\n]") ?
				"\"" + text.Replace("\"", "\"\"") + "\"" : text;
		}

		public void ExportSave(DateTime Date, ExportType ExportType, string Headings) {
			MemoryStream m = new MemoryStream();
			StreamWriter w = new StreamWriter(m);
			string delim = ExportType == ExportType.CSV ? "," : "\t";
			Func<string, string> quote;
			if (ExportType == ExportType.CSV)
				quote = csvQuote;
			else
				quote = tabQuote;
			foreach(JObject f in Database.Query($@"SELECT {RCSession.RepairCafe.ExportFields}
FROM RepairFormView
WHERE RepairCafe = {Cafe}
AND Date = {Database.Quote(Date)}
ORDER BY FormNumber")) {
				if(Headings == "on") {
					w.WriteLine(string.Join(delim, f.Properties().Select(p => p.Name)));
					Headings = null;
				}
				w.WriteLine(string.Join(delim, f.Properties().Select(p => quote(p.Value.ToString()))));
			}
			m.Position = 0;
			Response.AddHeader("Content-Disposition", $"attachment; filename=\"{RCSession.RepairCafe.Name}-{Date:yyyy-mm-dd}.{(ExportType == ExportType.CSV ? "csv" : "txt")}\"");
			WriteResponse(m, ExportType == ExportType.CSV ? "text/csv" : "application/vnd.ms-excel", System.Net.HttpStatusCode.OK);
		}

		public DataTableForm Contacts() {
			if (!GetCurrentCafe())
				return null;
			DataTableForm form = new DataTableForm(this, typeof(Contact));
			form.Select = "/home/editcontact";
			form["Type"].MakeSelectable(ContactType.Select(Database));
			InsertMenuOption(new MenuOption("New Contact", "/home/editcontact?id=0"));
			return form;
		}

		public JObjectEnumerable ContactsListing() {
			string sql = $"SELECT * FROM Contact WHERE RepairCafe = {Cafe}";
			string t = GetParameters["type"];
			if (!string.IsNullOrEmpty(t))
				sql += " AND Type = " + int.Parse(t);
			return Database.Query(sql);
		}

		public Form EditContact(int id) {
			if (!GetCurrentCafe())
				return null;
			Form form = new Form(this, typeof(Contact));
			form["Type"].MakeSelectable(ContactType.Select(Database));
			if (id > 0) {
				Utils.Check(Database.TryGet(id, out Contact c), "Contact not found");
				form.Data = c;
				form.CanDelete = true;
			} else
				form.Data = new Contact() { RepairCafe = Cafe };
			return form;
		}

		public AjaxReturn EditContactSave(Contact json) {
			return SaveRecord(json);
		}

		public AjaxReturn EditContactDelete(int id) {
			return DeleteRecord("Contact", id);
		}

	}
}
