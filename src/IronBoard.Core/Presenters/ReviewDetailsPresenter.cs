﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronBoard.Core.Views;
using IronBoard.RBWebApi.Model;

namespace IronBoard.Core.Presenters
{
   public delegate string GetDiff();

   class ReviewDetailsPresenter
   {
      private readonly IReviewDetailsView _view;
      private readonly GetDiff _getDiff;
      private string _lastDiff;

      public ReviewDetailsPresenter(IReviewDetailsView view, GetDiff getDiff)
      {
         _view = view;
         _getDiff = getDiff;
      }

      public void GenerateDiff()
      {
         if (_lastDiff == null)
         {
             _lastDiff = _getDiff();
         }
      }

      public void OpenLastDiff()
      {
         if (_lastDiff == null) return;

         string targetPath = Path.Combine(Path.GetTempPath(), "ironboard-last.diff");
         File.WriteAllText(targetPath, _lastDiff, Encoding.UTF8);
         IbApplication.OpenFile(targetPath);
      }

      private static IEnumerable<User> _users;
      private static IEnumerable<UserGroup> _groups;
      public IEnumerable<User> Users
      {
         get { return _users ?? (_users = IbApplication.RbClient.GetUsers()); }
      }
      public IEnumerable<UserGroup> Groups
      {
         get { return _groups ?? (_groups = IbApplication.RbClient.GetGroups()); }
      }

      private Repository Repository
      {
         get
         {
            string uriOrName = IbApplication.Config.Repository.Trim();

            Repository repository = IbApplication.RbClient.GetRepositories()
               .FirstOrDefault(
                  r => (r.Name != null && r.Name.Equals(uriOrName, StringComparison.InvariantCultureIgnoreCase)) ||
                       (r.Name != null && r.Path.Equals(uriOrName, StringComparison.InvariantCultureIgnoreCase)));

            if (repository == null)
            {
               throw new ApplicationException(string.Format("could not find repository by name or path, .reviewboarc value: {0}, available repositories: [{1}]",
                  uriOrName,
                  string.Join("; ", IbApplication.RbClient.GetRepositories().Select(ir => ir.Path))));
            }

            return repository;
         }
      }

      public void PostReview(Review r)
      {
         Task.Factory.StartNew(() =>
            {
               Exception ex = null;
               try
               {
                  _view.UpdatePostStatus(Strings.PostProgress_DetectRepository);
                  r.Repository = Repository;

                  _view.UpdatePostStatus(Strings.PostProgress_MainTicket);
                  IbApplication.RbClient.Post(r);

                  _view.UpdatePostStatus(Strings.PostProgress_Diff);
                  IbApplication.RbClient.AttachDiff(r, IbApplication.CodeRepository.RelativeRoot, _lastDiff);
                  IbApplication.RbClient.MakePublic(r);
               }
               catch (Exception ex1)
               {
                  ex = ex1;
               }
               _view.UpdatePostFinish(ex);
            });
      }
   }
}
