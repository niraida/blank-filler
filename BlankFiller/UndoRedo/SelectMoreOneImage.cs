using BlankFiller.ViewModels;
using System;

namespace BlankFiller.UndoRedo
{
    class SelectMoreOneImage : IUndoRedoCommand
    {
        private PageViewModel _page;

        public SelectMoreOneImage(PageViewModel page)
        {
            if (page == null)
                throw new ArgumentNullException(nameof(page));
            _page = page;
        }

        public void Execute()
        {
            _page.IsSelected = true;
        }

        public void Rollback()
        {
            _page.IsSelected = false;
        }
    }
}
