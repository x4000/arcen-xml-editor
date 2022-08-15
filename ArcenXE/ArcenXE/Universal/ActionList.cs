using System.Collections;

namespace ArcenXE.Universal
{
    public sealed class ActionList<T> : IEnumerable<T>
    {
        private readonly List<T> internalList = new List<T>();
        private readonly Action<T>? actionOnAdd;
        private readonly Action<T>? actionOnRemove;

        public ActionList( Action<T>? actionOnAdd, Action<T>? actionOnRemove )
        {
            this.actionOnAdd = actionOnAdd;
            this.actionOnRemove = actionOnRemove;
        }

        public T this[int index]
        {
            get => this.internalList[index];
        }

        public void Add( T item )
        {
            this.actionOnAdd?.Invoke( item );
            this.internalList.Add( item );
        }

        public void AddRange( IEnumerable<T> items )
        {
            foreach ( T item in items )
                this.actionOnAdd?.Invoke( item );
            this.internalList.AddRange( items );
        }

        public void Remove( T item )
        {
            this.actionOnRemove?.Invoke( item );
            this.internalList.Remove( item );
        }

        public void RemoveAt( int index )
        {
            T item = this.internalList[index];
            this.actionOnRemove?.Invoke( item );
            this.internalList.RemoveAt( index );
        }

        public void Clear()
        {
            foreach ( T item in this.internalList )
                this.actionOnRemove?.Invoke( item );                
            this.internalList.Clear();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)this.internalList).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)this.internalList).GetEnumerator();
        }

        //public delegate void ActionOnAdd(); //in case I need a personalized delegate
    }
}
