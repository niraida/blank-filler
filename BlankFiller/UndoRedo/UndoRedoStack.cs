using ReactiveUI;
using System;
using System.Linq;
using System.Reactive.Linq;

namespace BlankFiller.UndoRedo
{
    /// <summary>
    /// Стек команд для Undo/Redo
    /// </summary>
    internal class UndoRedoStack : ReactiveObject
    {
        /// <summary>
        /// Список команд
        /// </summary>
        public ReactiveList<IUndoRedoCommand> Commands { get; private set; }
                
        /// <summary>
        /// Список команд которые были отменены но могут быть Redo
        /// </summary>
        public ReactiveList<IUndoRedoCommand> UndoedCommands { get; private set; }

        public ReactiveCommand Undo { get; set; }
        public ReactiveCommand Redo { get; set; }

        public UndoRedoStack()
        {
            Commands = new ReactiveList<IUndoRedoCommand>();
            UndoedCommands = new ReactiveList<IUndoRedoCommand>();
            
            Undo = ReactiveCommand.Create(UndoImplementation, Commands.Changed.Select(_ => Commands.Any()));
            Redo = ReactiveCommand.Create(RedoImplementation, UndoedCommands.Changed.Select(_ => UndoedCommands.Any()));
        }

        /// <summary>
        /// Выполняет новую команду
        /// </summary>
        /// <param name="command">команда</param>
        public void ExecuteNewCommand(IUndoRedoCommand command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));            

            if (UndoedCommands.Any())
                UndoedCommands.Clear();

            Commands.Add(command);
            command.Execute();
        }

        /// <summary>
        /// Команда Отменить
        /// </summary>
        private void UndoImplementation()
        {
            if (!Commands.Any())
                return;

            var commandToUndo = Commands[Commands.Count - 1];
            commandToUndo.Rollback();
            Commands.RemoveAt(Commands.Count - 1);
            UndoedCommands.Add(commandToUndo);
        }

        /// <summary>
        /// Команда Вернуть
        /// </summary>
        private void RedoImplementation()
        {
            if (!UndoedCommands.Any())
                return;

            var commandToRedo = UndoedCommands[Commands.Count - 1];
            commandToRedo.Execute();
            UndoedCommands.RemoveAt(Commands.Count - 1);
            Commands.Add(commandToRedo);
        }

        internal void Clear()
        {
            UndoedCommands.Clear();
            Commands.Clear();
        }
    }
}
