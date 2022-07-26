namespace ArcenXE.Universal
{
    public class SuperBasicPool<T> where T : Control, new()
    {
        private readonly List<T> innerList = new List<T>();

        public T GetOrAdd()
        {
            if ( innerList.Count > 0 )
            {
                T item = innerList[^1];
                innerList.RemoveAt( innerList.Count - 1 );
                return item;
            }
            else
            {
                T item = new T();
                return item;
            }
        }

        public void ReturnToPool( T item )
        {
            innerList.Add( item );
        }
    }
}
