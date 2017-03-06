using BlankFiller.Models;
using BlankFiller.ViewModels;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace BlankFiller.UndoRedo
{
    class ChangeTextBlockListCommand : IUndoRedoCommand
    {
        private readonly PageViewModel _page;
        private readonly ReadOnlyCollection<TextBlock> _original;
        private readonly ReadOnlyCollection<TextBlock> _newTextBlocks;

        public ChangeTextBlockListCommand(PageViewModel page, List<TextBlock> newTextBlocks)
        {
            _page = page;
            _original = new ReadOnlyCollection<TextBlock>(_page.TextBlocks.Select(x => (TextBlock)x.Clone()).ToList());
            _newTextBlocks = new ReadOnlyCollection<TextBlock>(newTextBlocks.Select(x => (TextBlock)x.Clone()).ToList());
        }

        public void Execute()
        {
            _page.TextBlocks = _newTextBlocks;
        }

        public void Rollback()
        {
            _page.TextBlocks = _original;
        }
    }
}
