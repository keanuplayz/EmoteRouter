using System.Collections;
using System.Collections.Generic;

namespace EmoteRouter
{
    public class ThreadSafeList<T> : IList<T>
    {
        protected List<T> InternalList = new List<T>();

        public IEnumerator<T> GetEnumerator()
        {
            return Copy().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        
        public T this[int index]
        {
            get => Copy()[index];  
            set 
            {
                lock(InternalList)
                {
                    InternalList[index] = value;
                }
            }
        }

        public void Add(T item)
        {
            lock(InternalList)
            {
                InternalList.Add(item);
            }
        }

        public void RemoveAt(int index)
        {
            lock(InternalList)
            {
                InternalList.RemoveAt(index);
            }
        }

        public void Clear()
        {
            lock(InternalList)
            {
                InternalList.Clear();
            }
        }

        public void Insert(int index, T item)
        {
            lock(InternalList)
            {          
                InternalList.Insert(index, item);
            }
        }

        public int IndexOf(T item)
        {
            lock(InternalList)
            {
                return InternalList.IndexOf(item);
            }
        }

        public bool Contains(T item)
        {
            lock(InternalList)
            {
                return InternalList.Contains(item);
            }
        }

        public void CopyTo(T[] array, int index)
        {
            lock(InternalList)
            {
                InternalList.CopyTo(array, index);
            }
        }

        public bool Remove(T item)
        {
            lock(InternalList)
            {
                return InternalList.Remove(item);
            }
        }

        public int Count
        {
            get
            {
                 lock(InternalList)
                {
                    return InternalList.Count;
                }
            }
        }

        public bool IsReadOnly => false;    

        public List<T> Copy()
        {
            lock(InternalList)
            {
                return new List<T>(InternalList);
            }
        }
    }
}