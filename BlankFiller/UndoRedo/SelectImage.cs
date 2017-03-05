using BlankFiller.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlankFiller.UndoRedo
{
    class SelectImage : IUndoRedoCommand
    {
        private PageViewModel _page;
        private List<PageViewModel> _selectedPages;

        public SelectImage(ICollection<PageViewModel> allPages, PageViewModel page)
        {
            if (page == null)
                throw new ArgumentNullException(nameof(page));
            if (allPages == null)
                throw new ArgumentNullException(nameof(allPages));           
            _page = page;
            _selectedPages = allPages.Where(x => x.IsSelected).ToList();
        }

        public void Execute()
        {
            _selectedPages.ForEach(x => x.IsSelected = false);
            _page.IsSelected = true;
        }

        public void Rollback()
        {
            _page.IsSelected = false;
            _selectedPages.ForEach(x => x.IsSelected = true);
        }
    }
}
