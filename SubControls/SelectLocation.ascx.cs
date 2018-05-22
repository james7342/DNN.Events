using System.Diagnostics;
using System.Web.UI.WebControls;
using System.Collections;
using DotNetNuke.Services.Localization;
using System;
using DotNetNuke.Security;


#region Copyright
// 
// DotNetNuke® - http://www.dotnetnuke.com
// Copyright (c) 2002-2018
// by DotNetNuke Corporation
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and 
// to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions 
// of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
//
#endregion


namespace DotNetNuke.Modules.Events
	{
		
		public partial class SelectLocation : EventBase
		{
			
#region Properties
			
			private ArrayList _selectedLocation = new ArrayList();
			private bool _gotLocations = false;
			private ArrayList _modulelocationids = new ArrayList();
			public ArrayList SelectedLocation
			{
				get
				{
					//have selected the location before
					if (!_gotLocations)
					{
						_gotLocations = true;
						_selectedLocation.Clear();
						
						//is there a default module location when location select has been disabled
						//if not has it been passed in as a parameter
						//if not is there a default module location when location select has not been disabled
						//if not is there as setting in cookies available
						if (Settings.Enablelocations == EventModuleSettings.DisplayLocations.DoNotDisplay)
						{
							if (Settings.ModuleLocationsSelected == EventModuleSettings.LocationsSelected.All)
							{
								_selectedLocation.Clear();
								_selectedLocation.Add("-1");
							}
							else
							{
								_selectedLocation.Clear();
								foreach (int location in Settings.ModuleLocationIDs)
								{
									_selectedLocation.Add(location);
								}
							}
						}
						else if (!(Request.Params["Location"] == null))
						{
							PortalSecurity objSecurity = new PortalSecurity();
							string tmpLocation = Request.Params["Location"];
							tmpLocation = objSecurity.InputFilter(tmpLocation, PortalSecurity.FilterFlag.NoScripting);
							tmpLocation = objSecurity.InputFilter(tmpLocation, PortalSecurity.FilterFlag.NoSQL);
							EventLocationController oCntrlEventLocation = new EventLocationController();
							EventLocationInfo oEventLocation = oCntrlEventLocation.EventsLocationGetByName(tmpLocation, PortalSettings.PortalId);
							if (!ReferenceEquals(oEventLocation, null))
							{
								_selectedLocation.Add(oEventLocation.Location);
							}
						}
						else if (Settings.ModuleLocationsSelected != EventModuleSettings.LocationsSelected.All)
						{
							_selectedLocation.Clear();
							foreach (int location in Settings.ModuleLocationIDs)
							{
								_selectedLocation.Add(location);
							}
						}
						else if (ReferenceEquals(Request.Cookies["DNNEvents"], null))
						{
							_selectedLocation.Clear();
							_selectedLocation.Add("-1");
						}
						else
						{
							//Do we have a special one for this module
							if (ReferenceEquals(Request.Cookies["DNNEvents"]["EventLocation" + System.Convert.ToString(ModuleId)], null))
							{
								_selectedLocation.Clear();
								_selectedLocation.Add("-1");
							}
							else
							{
								//Yes there is one!
								PortalSecurity objSecurity = new PortalSecurity();
								string tmpLocation = System.Convert.ToString(Request.Cookies["DNNEvents"]["EventLocation" + System.Convert.ToString(ModuleId)]);
								tmpLocation = objSecurity.InputFilter(tmpLocation, PortalSecurity.FilterFlag.NoScripting);
								tmpLocation = objSecurity.InputFilter(tmpLocation, PortalSecurity.FilterFlag.NoSQL);
								string[] tmpArray = tmpLocation.Split(',');
								for (int i = 0; i <= tmpArray.Length - 1; i++)
								{
									if (tmpArray[i] != "")
									{
										_selectedLocation.Add(int.Parse(tmpArray[i]));
									}
								}
							}
						}
					}
					return _selectedLocation;
				}
				set
				{
					try
					{
						_selectedLocation = value;
						_gotLocations = true;
						Response.Cookies["DNNEvents"]["EventLocation" + System.Convert.ToString(ModuleId)] = string.Join(",", (string[]) (_selectedLocation.ToArray(typeof(string))));
						Response.Cookies["DNNEvents"].Expires = DateTime.Now.AddMinutes(2);
						Response.Cookies["DNNEvents"].Path = "/";
					}
					catch (Exception)
					{
					}
				}
			}
			public ArrayList ModuleLocationIDs
			{
				get
				{
					return _modulelocationids;
				}
				set
				{
					_modulelocationids = value;
				}
			}
#endregion
			
#region Public Methods
			public void StoreLocations()
			{
				SelectedLocation.Clear();
				ArrayList lstLocations = new ArrayList();
				if (Settings.Enablelocations == EventModuleSettings.DisplayLocations.SingleSelect)
				{
					lstLocations.Add(ddlLocations.SelectedValue);
				}
				else
				{
					if (ddlLocations.CheckedItems.Count != ddlLocations.Items.Count)
					{
						foreach (Telerik.Web.UI.RadComboBoxItem item in ddlLocations.CheckedItems)
						{
							lstLocations.Add(item.Value);
						}
					}
					else
					{
						lstLocations.Add("-1");
					}
				}
				SelectedLocation = lstLocations;
			}
			
#endregion
			
#region Event Handlers
			private void Page_Load(System.Object sender, EventArgs e)
			{
				try
				{
					// Add the external Validation.js to the Page
					const string csname = "ExtValidationScriptFile";
					Type cstype = System.Reflection.MethodBase.GetCurrentMethod().GetType();
					string cstext = "<script src=\"" + ResolveUrl("~/DesktopModules/Events/Scripts/Validation.js") + "\" type=\"text/javascript\"></script>";
					if (!Page.ClientScript.IsClientScriptBlockRegistered(csname))
					{
						Page.ClientScript.RegisterClientScriptBlock(cstype, csname, cstext, false);
					}
					
					ddlLocations.EmptyMessage = Localization.GetString("NoLocations", LocalResourceFile);
					ddlLocations.Localization.AllItemsCheckedString = Localization.GetString("AllLocations", LocalResourceFile);
					ddlLocations.Localization.CheckAllString = Localization.GetString("SelectAllLocations", LocalResourceFile);
					if (Settings.Enablelocations == EventModuleSettings.DisplayLocations.SingleSelect)
					{
						ddlLocations.CheckBoxes = false;
					}
					
					if (!Page.IsPostBack)
					{
						//Bind DDL
						EventLocationController ctrlEventLocations = new EventLocationController();
						ArrayList lstLocations = ctrlEventLocations.EventsLocationList(PortalId);
						
						ArrayList arrLocations = new ArrayList();
						if (Settings.Restrictlocations)
						{
							foreach (EventLocationInfo dbLocation in lstLocations)
							{
								foreach (int location in Settings.ModuleLocationIDs)
								{
									if (dbLocation.Location == location)
									{
										arrLocations.Add(dbLocation);
									}
								}
							}
						}
						else
						{
							arrLocations.AddRange(lstLocations);
						}
						
						if (lstLocations.Count == 0)
						{
							Visible = false;
							SelectedLocation.Clear();
							return;
						}
						
						//Restrict locations by events in time frame.
						if (Settings.RestrictLocationsToTimeFrame)
						{
							//Only for list view.
							string whichView = string.Empty;
							if (!(Request.QueryString["mctl"] == null) && ModuleId == System.Convert.ToInt32(Request.QueryString["ModuleID"]))
							{
								if (Request["mctl"].EndsWith(".ascx"))
								{
									whichView = Request["mctl"];
								}
								else
								{
									whichView = Request["mctl"] +".ascx";
								}
							}
							if (whichView.Length == 0)
							{
								if (!ReferenceEquals(Request.Cookies.Get("DNNEvents" + System.Convert.ToString(ModuleId)), null))
								{
									whichView = Request.Cookies.Get("DNNEvents" + System.Convert.ToString(ModuleId)).Value;
								}
								else
								{
									whichView = Settings.DefaultView;
								}
							}
							
							if (whichView == "EventList.ascx" || whichView == "EventRpt.ascx")
							{
								EventInfoHelper objEventInfoHelper = new EventInfoHelper(ModuleId, TabId, PortalId, Settings);
								ArrayList lstEvents = default(ArrayList);
								
								bool getSubEvents = Settings.MasterEvent;
								int numDays = Settings.EventsListEventDays;
								DateTime displayDate = default(DateTime);
								DateTime startDate = default(DateTime);
								DateTime endDate = default(DateTime);
								if (Settings.ListViewUseTime)
								{
									displayDate = DisplayNow();
								}
								else
								{
									displayDate = DisplayNow().Date;
								}
								if (Settings.EventsListSelectType == "DAYS")
								{
									startDate = displayDate.AddDays(Settings.EventsListBeforeDays * -1);
									endDate = displayDate.AddDays(Settings.EventsListAfterDays * 1);
								}
								else
								{
									startDate = displayDate;
									endDate = displayDate.AddDays(numDays);
								}
								
								lstEvents = objEventInfoHelper.GetEvents(startDate, endDate, getSubEvents, 
									new ArrayList(System.Convert.ToInt32(new[] {"-1"})), new ArrayList(System.Convert.ToInt32(new[] {"-1"})), -1, -1);
								
								ArrayList eventLocationIds = new ArrayList();
								foreach (EventInfo lstEvent in lstEvents)
								{
									eventLocationIds.Add(lstEvent.Location);
								}
								foreach (EventLocationInfo lstLocation in lstLocations)
								{
									if (!eventLocationIds.Contains(lstLocation.Location))
									{
										arrLocations.Remove(lstLocation);
									}
								}
							}
						}
						
						//Bind locations.
						ddlLocations.DataSource = arrLocations;
						ddlLocations.DataBind();
						
						if (Settings.Enablelocations == EventModuleSettings.DisplayLocations.SingleSelect)
						{
							ddlLocations.Items.Insert(0, new Telerik.Web.UI.RadComboBoxItem(Localization.GetString("AllLocations", LocalResourceFile), "-1"));
							ddlLocations.SelectedIndex = 0;
						}
						ddlLocations.OnClientDropDownClosed = "function() { btnUpdateClick('" + btnUpdate.UniqueID + "','" + ddlLocations.ClientID + "');}";
						ddlLocations.OnClientLoad = "function() { storeText('" + ddlLocations.ClientID + "');}";
						if (Settings.Enablelocations == EventModuleSettings.DisplayLocations.SingleSelect)
						{
							foreach (int location in SelectedLocation)
							{
								ddlLocations.SelectedIndex = ddlLocations.FindItemByValue(location.ToString()).Index;
								break;
							}
						}
						else
						{
							foreach (int location in SelectedLocation)
							{
								foreach (Telerik.Web.UI.RadComboBoxItem item in ddlLocations.Items)
								{
									if (item.Value == location.ToString())
									{
										item.Checked = true;
									}
								}
							}
							
							if (System.Convert.ToInt32(SelectedLocation[0]) == -1)
							{
								foreach (Telerik.Web.UI.RadComboBoxItem item in ddlLocations.Items)
								{
									item.Checked = true;
								}
							}
						}
						
					}
				}
				catch (Exception)
				{
					//ProcessModuleLoadException(Me, exc)
				}
			}
			
			public void btnUpdate_Click(object sender, EventArgs e)
			{
				StoreLocations();
				
				// Fire the LocationSelected event...
				CommandEventArgs args = new CommandEventArgs(SelectedLocation.ToString(), null);
				if (LocationSelectedChangedEvent != null)
					LocationSelectedChangedEvent(this, args);
			}
			
			public delegate void LocationSelectedChangedEventHandler(object sender, CommandEventArgs e);
			private LocationSelectedChangedEventHandler LocationSelectedChangedEvent;
			
			public event LocationSelectedChangedEventHandler LocationSelectedChanged
			{
				add
				{
					LocationSelectedChangedEvent = (LocationSelectedChangedEventHandler) System.Delegate.Combine(LocationSelectedChangedEvent, value);
				}
				remove
				{
					LocationSelectedChangedEvent = (LocationSelectedChangedEventHandler) System.Delegate.Remove(LocationSelectedChangedEvent, value);
				}
			}
			
			
#endregion
			
#region  Web Form Designer Generated Code
			
			//This call is required by the Web Form Designer.
			[DebuggerStepThrough()]private void InitializeComponent()
			{
				
			}
			
			private void Page_Init(System.Object sender, EventArgs e)
			{
				//CODEGEN: This method call is required by the Web Form Designer
				//Do not modify it using the code editor.
				InitializeComponent();
			}
#endregion
			
		}
		
	}
	
