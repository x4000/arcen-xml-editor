using ArcenXE.Visualization.Utilities;
using System.ComponentModel;

namespace ArcenXE.Universal
{
    public class SuperBasicPool<T> where T : Control, new()
    {
        private readonly List<T> innerList = new List<T>();

        public T GetOrAdd( Action<T>? creationAction )
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
                item.Tag = new PooledControlTagInfo( item, () => { ReturnToPool( item ); } );
                creationAction?.Invoke( item );
                return item;
            }
        }

        public void ReturnToPool( T item )
        {
            innerList.Add( item );
        }
    }

    public class BasicComponentPool<T> where T : Component, new()
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

    public class PoolWithReference<T> where T : Component, new()
    {
        private readonly List<T> theForeverList = new List<T>();
        private readonly Queue<T> innerQueue = new Queue<T>();

        public T GetOrAdd()
        {
            if ( innerQueue.Count > 0 )
            {
                {
                    if ( innerQueue.TryDequeue( out T? item ) )
                    {
                        if ( item != null )
                        {
                            if ( theForeverList != null )
                                theForeverList.Add( item );
                            return item;
                        }
                    }
                }
                {
                    T item = new T();
                    if ( theForeverList != null )
                        theForeverList.Add( item );
                    return item;
                }
            }
            else
            {
                T item = new T();
                if ( theForeverList != null )
                    theForeverList.Add( item );
                return item;
            }
        }

        public void ReturnAllToPool()
        {
            innerQueue.Clear();
            foreach ( T item in theForeverList )
            {
                if ( item == null )
                    continue;
                innerQueue.Enqueue( item );
            }
        }

        /*public void ReturnToPool( T item )
        {
            if ( item == null )
                return;

            this.innerQueue.Enqueue( item );
            this.theForeverList.TryDequeue( out item );
        }*/
    }
}
