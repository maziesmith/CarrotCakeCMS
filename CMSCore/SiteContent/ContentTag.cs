﻿using System;
using System.Collections.Generic;
using System.Linq;
using Carrotware.CMS.Data;

/*
* CarrotCake CMS
* http://www.carrotware.com/
*
* Copyright 2011, Samantha Copeland
* Dual licensed under the MIT or GPL Version 3 licenses.
*
* Date: October 2011
*/

namespace Carrotware.CMS.Core {

	public class ContentTag : IContentMetaInfo {

		public ContentTag() { }

		public Guid ContentTagID { get; set; }
		public Guid SiteID { get; set; }
		public string TagText { get; set; }
		public string TagSlug { get; set; }
		public string TagURL { get; set; }
		public int? UseCount { get; set; }
		public int? PublicUseCount { get; set; }
		public bool IsPublic { get; set; }
		public DateTime? EditDate { get; set; }

		public override bool Equals(Object obj) {
			//Check for null and compare run-time types.
			if (obj == null || GetType() != obj.GetType()) return false;

			if (obj is ContentTag) {
				ContentTag p = (ContentTag)obj;
				return (this.SiteID == p.SiteID && this.ContentTagID == p.ContentTagID);
			} else {
				return false;
			}
		}

		public override int GetHashCode() {
			return this.ContentTagID.GetHashCode() ^ this.SiteID.GetHashCode();
		}

		internal ContentTag(vw_carrot_TagCounted c) {
			if (c != null) {
				this.ContentTagID = c.ContentTagID;
				this.SiteID = c.SiteID;
				this.TagSlug = ContentPageHelper.ScrubSlug(c.TagSlug);
				this.TagText = c.TagText;
				this.UseCount = c.UseCount;
				this.PublicUseCount = 1;
				this.IsPublic = c.IsPublic;

				SiteData site = SiteData.GetSiteFromCache(c.SiteID);
				if (site != null) {
					this.TagURL = ContentPageHelper.ScrubFilename(c.ContentTagID, String.Format("/{0}/{1}.aspx", site.BlogTagPath, c.TagSlug.Trim()));
				}
			}
		}

		internal ContentTag(vw_carrot_TagURL c) {
			if (c != null) {
				SiteData site = SiteData.GetSiteFromCache(c.SiteID);

				this.ContentTagID = c.ContentTagID;
				this.SiteID = c.SiteID;
				this.TagURL = ContentPageHelper.ScrubFilename(c.ContentTagID, c.TagUrl);
				this.TagText = c.TagText;
				this.UseCount = c.UseCount;
				this.PublicUseCount = c.PublicUseCount;
				this.IsPublic = c.IsPublic;

				if (c.EditDate.HasValue) {
					this.EditDate = site.ConvertUTCToSiteTime(c.EditDate.Value);
				}
			}
		}

		internal ContentTag(carrot_ContentTag c) {
			if (c != null) {
				this.ContentTagID = c.ContentTagID;
				this.SiteID = c.SiteID;
				this.TagSlug = ContentPageHelper.ScrubSlug(c.TagSlug);
				this.TagText = c.TagText;
				this.IsPublic = c.IsPublic;
				this.UseCount = 1;
				this.PublicUseCount = 1;

				SiteData site = SiteData.GetSiteFromCache(c.SiteID);
				if (site != null) {
					this.TagURL = ContentPageHelper.ScrubFilename(c.ContentTagID, String.Format("/{0}/{1}.aspx", site.BlogTagPath, c.TagSlug.Trim()));
				}
			}
		}

		public static ContentTag Get(Guid TagID) {
			ContentTag _item = null;
			using (CarrotCMSDataContext _db = CarrotCMSDataContext.GetDataContext()) {
				carrot_ContentTag query = CompiledQueries.cqGetContentTagByID(_db, TagID);
				if (query != null) {
					_item = new ContentTag(query);
				}
			}

			return _item;
		}

		public static ContentTag GetByURL(Guid SiteID, string requestedURL) {
			ContentTag _item = null;
			using (CarrotCMSDataContext _db = CarrotCMSDataContext.GetDataContext()) {
				vw_carrot_TagURL query = CompiledQueries.cqGetContentTagByURL(_db, SiteID, requestedURL);
				if (query != null) {
					_item = new ContentTag(query);
				}
			}

			return _item;
		}

		public static int GetSimilar(Guid SiteID, Guid TagID, string tagSlug) {
			using (CarrotCMSDataContext _db = CarrotCMSDataContext.GetDataContext()) {
				IQueryable<carrot_ContentTag> query = CompiledQueries.cqGetContentTagNoMatch(_db, SiteID, TagID, tagSlug);

				return query.Count();
			}
		}

		public static int GetSiteCount(Guid siteID) {
			int iCt = -1;

			using (CarrotCMSDataContext _db = CarrotCMSDataContext.GetDataContext()) {
				iCt = CompiledQueries.cqGetContentTagCountBySiteID(_db, siteID);
			}

			return iCt;
		}

		public static List<ContentTag> BuildTagList(Guid rootContentID) {
			List<ContentTag> _types = null;

			using (CarrotCMSDataContext _db = CarrotCMSDataContext.GetDataContext()) {
				IQueryable<carrot_ContentTag> query = CompiledQueries.cqGetContentTagByContentID(_db, rootContentID);

				_types = (from d in query.ToList()
						  select new ContentTag(d)).ToList();
			}

			return _types;
		}

		public void Save() {
			using (CarrotCMSDataContext _db = CarrotCMSDataContext.GetDataContext()) {
				bool bNew = false;
				carrot_ContentTag s = CompiledQueries.cqGetContentTagByID(_db, this.ContentTagID);

				if (s == null || (s != null && s.ContentTagID == Guid.Empty)) {
					s = new carrot_ContentTag();
					s.ContentTagID = Guid.NewGuid();
					s.SiteID = this.SiteID;
					bNew = true;
				}

				s.TagSlug = ContentPageHelper.ScrubSlug(this.TagSlug);
				s.TagText = this.TagText;
				s.IsPublic = this.IsPublic;

				if (bNew) {
					_db.carrot_ContentTags.InsertOnSubmit(s);
				}

				_db.SubmitChanges();

				this.ContentTagID = s.ContentTagID;
			}
		}

		public void Delete() {
			using (CarrotCMSDataContext _db = CarrotCMSDataContext.GetDataContext()) {
				carrot_ContentTag s = CompiledQueries.cqGetContentTagByID(_db, this.ContentTagID);

				if (s != null) {
					IQueryable<carrot_TagContentMapping> lst = (from m in _db.carrot_TagContentMappings
																where m.ContentTagID == s.ContentTagID
																select m);

					_db.carrot_TagContentMappings.BatchDelete(lst);
					_db.carrot_ContentTags.DeleteOnSubmit(s);
					_db.SubmitChanges();
				}
			}
		}

		#region IContentMetaInfo Members

		public void SetValue(Guid ContentMetaInfoID) {
			this.ContentTagID = ContentMetaInfoID;
		}

		public Guid ContentMetaInfoID {
			get { return this.ContentTagID; }
		}

		public string MetaInfoText {
			get { return this.TagText; }
		}

		public string MetaInfoURL {
			get { return this.TagURL; }
		}

		public bool MetaIsPublic {
			get { return this.IsPublic; }
		}

		public DateTime? MetaDataDate {
			get { return this.EditDate; }
		}

		public int MetaInfoCount {
			get { return this.UseCount == null ? 0 : Convert.ToInt32(this.UseCount); }
		}

		public int MetaPublicInfoCount {
			get { return this.PublicUseCount == null ? 0 : Convert.ToInt32(this.PublicUseCount); }
		}

		#endregion IContentMetaInfo Members
	}
}