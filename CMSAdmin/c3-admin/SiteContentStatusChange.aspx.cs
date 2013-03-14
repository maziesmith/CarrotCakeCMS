﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;
using Carrotware.CMS.Core;
/*
* CarrotCake CMS
* http://www.carrotware.com/
*
* Copyright 2011, Samantha Copeland
* Dual licensed under the MIT or GPL Version 2 licenses.
*
* Date: October 2011
*/

namespace Carrotware.CMS.UI.Admin.c3_admin {
	public partial class SiteContentStatusChange : AdminBasePage {

		protected void Page_Load(object sender, EventArgs e) {
			Master.ActivateTab(AdminBaseMasterPage.SectionID.StatusChange);

			SiteData site = siteHelper.GetCurrentSite();
			if (site == null) {
				Response.Redirect(SiteFilename.DashboardURL);
			}

			if (!IsPostBack) {
				txtDate.Text = SiteData.CurrentSite.Now.ToShortDateString();
				ContentPage cp = pageHelper.GetLatestPosts(SiteData.CurrentSiteID, 2, false).FirstOrDefault();
				if (cp != null) {
					txtDate.Text = cp.GoLiveDate.ToShortDateString();
				}

				LoadGrid();
			}
		}


		protected void SetGrid(bool bAll, DateTime dateRange, int dateRangeDays) {
			List<ContentPage> lstContent = null;
			if (bAll) {
				dateRangeDays = -1;
			}
			ContentPageType.PageType pageType = ContentPageType.PageType.Unknown;

			if (rdoPost.Checked) {
				pageType = ContentPageType.PageType.BlogEntry;
			}
			if (rdoPage.Checked) {
				pageType = ContentPageType.PageType.ContentEntry;
			}

			lstContent = pageHelper.GetContentByDateRange(SiteID, dateRange, dateRangeDays, pageType,
				GeneralUtilities.GetNullableBoolValue(ddlActive), GeneralUtilities.GetNullableBoolValue(ddlSiteMap),
				GeneralUtilities.GetNullableBoolValue(ddlNavigation), GeneralUtilities.GetNullableBoolValue(ddlHide));

			lblPages.Text = string.Format(" {0} ", lstContent.Count);

			gvPages.DataSource = lstContent;
			gvPages.DataBind();
		}


		protected void btnSaveMapping_Click(object sender, EventArgs e) {
			List<Guid> lstUpd = new List<Guid>();

			foreach (GridViewRow row in gvPages.Rows) {
				var chkSelect = (CheckBox)row.FindControl("chkSelect");

				if (chkSelect.Checked) {
					var hdnContentID = (HiddenField)row.FindControl("hdnContentID");
					Guid gRoot = new Guid(hdnContentID.Value);

					lstUpd.Add(gRoot);
				}
			}

			string sAct = ddlAction.SelectedValue;

			if (sAct != "none") {
				ContentPageHelper.UpdateField fieldDev = ContentPageHelper.UpdateField.MarkActive;

				if (sAct == "searchengine") {
					fieldDev = ContentPageHelper.UpdateField.MarkAsIndexable;
				}
				if (sAct == "sitemap") {
					fieldDev = ContentPageHelper.UpdateField.MarkIncludeInSiteMap;
				}
				if (sAct == "navigation") {
					fieldDev = ContentPageHelper.UpdateField.MarkIncludeInSiteNav;
				}

				pageHelper.MarkSelectedPublished(SiteData.CurrentSiteID, lstUpd, fieldDev);
				LoadGrid();
			}
		}

		protected void btnFilter_Click(object sender, EventArgs e) {
			LoadGrid();
		}

		protected void rdoFilterResults_CheckedChanged(object sender, EventArgs e) {
			LoadGrid();
		}

		private void LoadGrid() {
			trFilter.Attributes["style"] = "display:none;";

			if (rdoFilterResults2.Checked) {
				trFilter.Attributes["style"] = "display:none;";
				SetGrid(true, SiteData.CurrentSite.Now, 0);
			} else {
				trFilter.Attributes["style"] = "";
				SetGrid(false, Convert.ToDateTime(txtDate.Text), int.Parse(ddlDateRange.SelectedValue));
			}
		}

	}
}
