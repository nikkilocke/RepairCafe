using System;
using System.Collections.Generic;
using System.Text;
using CodeFirstWebFramework;
using Newtonsoft.Json.Linq;

namespace RepairCafe {
	[Table]
	public class RepairCafe : JsonObject {
		const string defaultEntryFields = "Date,Name,Email,Phone,Address,ItemType,ItemForRepair,Brand,ModelNo,WhatIsWrong,CanContact,Photos,Repairer,Repaired,Happy,RepairerComments";
		const string defaultExportFields = "FormNumber,Name,Email,Phone,Address,PostCode,CanContact,Photos,TypeOfItem,ItemForRepair,Brand,ModelNo,WhatIsWrong,Repairer,RepairStatus,RepairerComments,Happy,CustomerComments";
		const string defaultItemTypes = "Electrical,Garment,Furniture/Ornament,Bicycle,Jewellery,IT/Phone,Other";
		[Primary]
		public int? idRepairCafe;
		[Unique("Name")]
		public string Name;
		[Length(0)]
		[DefaultValue(defaultEntryFields)]
		public string FormEntryFields = defaultEntryFields;
		[Length(0)]
		[DefaultValue(defaultExportFields)]
		public string ExportFields = defaultExportFields;
		[Length(0)]
		[DefaultValue(defaultItemTypes)]
		public string ItemTypes = defaultItemTypes;
	}
	[Table]
	public class ContactType : JsonObject {
		public const int Visitor = 1;
		public const int Repairer = 2;
		public const int Volunteer = 3;
		[Primary]
		public int? idContactType;
		[Unique("TypeOfContact")]
		public string TypeOfContact;
		public static JObjectEnumerable Select(CodeFirstWebFramework.Database Database) {
			return Database.Query("SELECT idContactType AS id, TypeOfContact AS value FROM ContactType ORDER BY TypeOfContact");
		}
	}

	[Table]
	public class Contact : JsonObject {
		[Primary]
		public int? idContact;
		[ForeignKey("RepairCafe")]
		[Unique("Name", 1)]
		public int RepairCafe;
		[Unique("Name", 3)]
		public string Name;
		public string FullName;
		public string Email;
		[Length(0)]
		public string Address;
		public string Postcode;
		public string Phone;
		public bool CanContact;
		[ForeignKey("ContactType")]
		[Unique("Name", 2)]
		public int Type;
	}

	[Table]
	public class ItemType : JsonObject {
		[Primary]
		public int? idItemType;
		[Unique("TypeOfItem", 1)]
		[DefaultValue("1")]
		public int RepairCafeId;
		[Unique("TypeOfItem", 2)]
		public string TypeOfItem;
		public int Sequence;
	}

	[Table]
	public class RepairStatus : JsonObject {
		[Primary]
		public int idRepairStatus;
		[Unique("Status")]
		public string Status;
	}

	[Table]
	public class RepairForm : JsonObject {
		[Primary]
		public int? idRepairForm;
		[ForeignKey("RepairCafe")]
		public int RepairCafe;
		public DateTime Date;
		public int FormNumber;
		public bool PreviousVisitor;
		public string Name;
		public string Email;
		public bool CanContact;
		[Length(0)]
		public string Address;
		public string Postcode;
		public string Phone;
		public bool Photos;
		public string ItemForRepair;
		[ForeignKey("ItemType")]
		public int ItemType;
		public string Brand;
		public string ModelNo;
		[Length(0)]
		public string WhatIsWrong;
		public bool Happy;
		[ForeignKey("RepairStatus")]
		public int Repaired;
		[Length(0)]
		public string CustomerComments;
		public string Repairer;
		[Length(0)]
		public string RepairerComments;
	}

	[View(@"SELECT RepairForm.*, RepairCafe.Name AS RepairCafeName, ItemType.TypeOfItem, RepairStatus.Status AS RepairStatus
FROM RepairForm
JOIN RepairCafe ON idRepairCafe = RepairCafe
JOIN ItemType ON RepairCafeId = RepairCafe AND idItemType = ItemType
JOIN RepairStatus ON idRepairStatus = Repaired")]
	public class RepairFormView : RepairForm {
		public string RepairCafeName;
		public string TypeOfitem;
		public string RepairStatus;
	}

}
