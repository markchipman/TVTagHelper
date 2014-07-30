﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace TVTagHelper.Models
{
    public class FileItem : INotifyPropertyChanged
    {
        public FileItem()
        {
            this.Id = Guid.NewGuid();
        }

        private string _title;
        public string Title 
        { 
            get { return _title; }
            set { SetField(ref _title, value, () => Title); }
        }

        private string _description;
        public string Description
        { 
            get { return _description; }
            set { SetField(ref _description, value, () => Description); }
        }

        private int _episodeNumber;
        public int EpisodeNumber
        { 
            get { return _episodeNumber; }
            set { SetField(ref _episodeNumber, value, () => EpisodeNumber); }
        }

        public Guid Id { get; set; }

        #region INotifyPropertyChanged Members & helpers

        //  We may want to look at Fody / PropertyChanged add-in to avoid this:
        //  http://stackoverflow.com/a/18002490/19020

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if(handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, string propertyName)
        {
            if(EqualityComparer<T>.Default.Equals(field, value))
                return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        //  These 2 functions were taken shamelessly from this SO article:
        //  http://stackoverflow.com/a/1316566/19020

        protected virtual void OnPropertyChanged<T>(Expression<Func<T>> selectorExpression)
        {
            if(selectorExpression == null)
                throw new ArgumentNullException("selectorExpression");
            MemberExpression body = selectorExpression.Body as MemberExpression;
            if(body == null)
                throw new ArgumentException("The body must be a member expression");
            OnPropertyChanged(body.Member.Name);
        }

        protected bool SetField<T>(ref T field, T value, Expression<Func<T>> selectorExpression)
        {
            if(EqualityComparer<T>.Default.Equals(field, value))
                return false;
            field = value;
            OnPropertyChanged(selectorExpression);
            return true;
        }

        #endregion
    }
}
