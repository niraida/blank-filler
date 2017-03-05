namespace BlankFiller.UndoRedo
{
    /// <summary>
    /// Команда для выполнения в UI
    /// </summary>
    internal interface IUndoRedoCommand
    {
        /// <summary>
        /// Выполняет команду
        /// </summary>
        void Execute();

        /// <summary>
        /// Отменяет команду
        /// </summary>
        void Rollback();
    }
}
