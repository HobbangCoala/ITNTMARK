using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ITNTCOMMON;
using System.Diagnostics;

#pragma warning disable 0219
#pragma warning disable 4014

namespace ITNTUTIL
{
    public class RingBuffer// : ICollection, IEnumerable
    {
        const int MAX_BUFFER_SIZE = 1024*1024;
        private int sp;
        private int ep;
        private int capacity = 1024;
        //private int capacity;
        //private int size;
        //private int head;
        //private int tail;
        private byte[] buffer;// = new byte[MAX_BUFFER_SIZE];
        private object lockobj = new object();

        //[NonSerialized()]
        //private object syncRoot;

        public RingBuffer(int capacity)
        {
            if (capacity > MAX_BUFFER_SIZE)
                this.capacity = MAX_BUFFER_SIZE;
            else if (capacity <= 0)
                this.capacity = 1024;
            else
                this.capacity = capacity;

            sp = 0;
            ep = 0;
            buffer = new byte[this.capacity];
        }

        //public RingBuffer(int capacity)
        //{
        //    this.capacity = capacity;
        //    size = 0;
        //    head = 0;
        //    tail = 0;
        //    buffer = new T[capacity];
        //}

        //public bool Contains(T item)
        //{
        //    int bufferIndex = head;
        //    var comparer = EqualityComparer<T>.Default;
        //    for (int i = 0; i < size; i++, bufferIndex++)
        //    {
        //        if (bufferIndex >= capacity)
        //            bufferIndex = 0;

        //        if (item == null && buffer[bufferIndex] == null)
        //            return true;
        //        else if ((buffer[bufferIndex] != null) &&
        //            comparer.Equals(buffer[bufferIndex], item))
        //            return true;
        //    }

        //    return false;
        //}


        public void Clear()
        {
            sp = 0;
            ep = 0;
            buffer.Initialize();
        }

        public int Put(byte[] src, int count)
        {
            string className = "RingBuffer";
            string funcName = "Put";
            try
            {
                lock (lockobj)
                {
                    if (sp >= capacity)
                        sp = sp % capacity;
                    ITNTTraceLog.Instance.Trace(3, "P1");
                    //Debug.WriteLine("P1-"+count.ToString());
                    //if ((count + GetSize()) > capacity)
                    //{
                    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - TOO LONG DATA(COUNT = {0}, SIZE = {1}", count, GetSize()));
                    //    return -0x10;
                    //}

                    //if((sp + count) > capacity)
                    //{
                    //    int copyleng1 = capacity - sp;
                    //    int copyleng2 = count - copyleng1;
                    //    Array.Copy(src, 0, buffer, sp, copyleng1);
                    //    //sp += copyleng1;
                    //    //if (sp >= capacity)
                    //    sp = (sp + copyleng1 + capacity) % capacity;
                    //    Array.Copy(src, copyleng1, buffer, sp, copyleng2);
                    //    sp = (sp + copyleng2 + capacity) % capacity;
                    //    //sp += copyleng2;
                    //    //if (sp >= capacity)
                    //    //    sp = 0;
                    //}
                    if ((sp + count) >= capacity)
                    {
                        int copyleng1 = capacity - sp;
                        int copyleng2 = count - copyleng1;
                        Array.Copy(src, 0, buffer, sp, copyleng1);
                        sp = 0;
                        Array.Copy(src, copyleng1, buffer, sp, copyleng2);
                        sp += copyleng2;
                    }
                    else
                    {
                        Array.Copy(src, 0, buffer, sp, count);
                        sp += count;
                    }

                    if (sp >= capacity)
                        sp = sp % capacity;
                }
                ITNTTraceLog.Instance.Trace(3, "P2");
                //Debug.WriteLine("P2-" + count.ToString());

                return count;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return ex.HResult;
            }
        }

        public int Put(byte item)
        {
            string className = "RingBuffer";
            string funcName = "Put";
            try
            {
                lock (lockobj)
                {
                    if (sp >= capacity)
                        sp = 0;
                    buffer[sp++] = item;
                    if (sp >= capacity)
                        sp = sp % capacity;
                }
            }
            catch(Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return ex.HResult;
            }
            return 1;
        }

        public int Get(ref byte[] dst, int count)
        {
            string className = "RingBuffer";
            string funcName = "Get";
            int retsize = 0;
            try
            {
                lock (lockobj)
                {
                    if (ep == sp)
                        return 0;
                    //Debug.WriteLine("G1-" + count.ToString());

                    if (sp > ep)
                    {
                        if ((ep + count) >= sp)
                        {
                            retsize = sp - ep;
                            Array.Copy(buffer, ep, dst, 0, retsize);
                            //ITNTTraceLog.Instance.TraceHex(2, "MarkComm::ReceiveCommData()1 : ", retsize, dst);
                        }
                        else
                        {
                            retsize = count;
                            Array.Copy(buffer, ep, dst, 0, retsize);
                            //ITNTTraceLog.Instance.TraceHex(2, "MarkComm::ReceiveCommData()2 : ", retsize, dst);
                        }
                    }
                    else
                    {
                        if (capacity >= (ep + count))
                        {
                            retsize = count;
                            Array.Copy(buffer, ep, dst, 0, retsize);
                            //ITNTTraceLog.Instance.TraceHex(2, "MarkComm::ReceiveCommData()3 : ", retsize, dst);
                        }
                        else
                        {
                            int copyleng1 = capacity - ep;
                            int copyleng2 = count - copyleng1;
                            Array.Copy(buffer, ep, dst, 0, copyleng1);
                            retsize = copyleng1;
                            if (copyleng2 >= sp)
                                copyleng2 = sp;
                            Array.Copy(buffer, 0, dst, copyleng1, copyleng2);
                            retsize += copyleng2;
                            //ITNTTraceLog.Instance.TraceHex(2, "MarkComm::ReceiveCommData()4 : ", retsize, dst);
                        }
                    }
                    ep = (ep + retsize + capacity) % capacity;
                    if (ep >= capacity)
                        ep = ep % capacity;
                    //Debug.WriteLine("G2-" + count.ToString());
                    //ITNTTraceLog.Instance.TraceHex(0, "MarkComm::ReceiveCommData()  SEND MARK :  ", retsize, dst);
                    return retsize;
                }

                //lock (lockobj)
                //{
                //    if (ep == sp)
                //        return 0;

                //    ITNTTraceLog.Instance.Trace(3, "G1");
                //    //int size = count;
                //    if ((ep < sp) && ((ep+count) > sp))
                //    {
                //        Array.Copy(buffer, ep, dst, 0, (sp-ep));
                //        ep = sp;
                //    }
                //    else
                //    {
                //        if ((ep + count) >= capacity)
                //        {
                //            int copyleng1 = capacity - ep;
                //            int copyleng2 = count - copyleng1;
                //            Array.Copy(buffer, ep, dst, 0, copyleng1);
                //            ep = (ep + copyleng1 + capacity) % capacity;
                //            Array.Copy(buffer, ep, dst, copyleng1, copyleng2);
                //            ep = (ep + copyleng2 + capacity) % capacity;
                //            //if (ep >= capacity)
                //            //    ep = 0;
                //        }
                //        else
                //        {
                //            Array.Copy(buffer, ep, dst, 0, count);
                //            ep = (ep + count + capacity) % capacity;
                //            //if (ep >= capacity)
                //            //    ep = 0;
                //        }
                //    }
                //    ITNTTraceLog.Instance.Trace(3, "G2");
                //    return count;
                //}
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                ITNTTraceLog.Instance.Trace(0, "G3");
                return ex.HResult;
            }
        }

        public int Get(ref byte value)
        {
            string className = "RingBuffer";
            string funcName = "Get";
            try
            {
                lock (lockobj)
                {
                    if (sp == ep)
                        return 0;
                    if (ep >= capacity)
                        ep = 0;
                    value = buffer[ep++];
                    if (ep >= capacity)
                        ep = ep % capacity;
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return ex.HResult;
            }
            return 1;
        }

        public int Look(ref byte[] dst, int count)
        {
            string className = "RingBuffer";
            string funcName = "Look";
            int retsize = 0;
            try
            {
                lock (lockobj)
                {
                    if (ep == sp)
                        return 0;
                    int tempep = ep;

                    ITNTTraceLog.Instance.Trace(3, "L1");
                    //Debug.WriteLine("L1-" + count.ToString());

                    if (sp >= tempep)
                    {
                        if ((tempep + count) >= sp)
                        {
                            retsize = sp - tempep;
                            Array.Copy(buffer, tempep, dst, 0, retsize);
                            tempep = sp;
                        }
                        else
                        {
                            retsize = count;
                            Array.Copy(buffer, tempep, dst, 0, retsize);
                            tempep = (tempep + count + capacity) % capacity;
                        }
                    }
                    else
                    {
                        if (capacity >= (tempep + count))
                        {
                            Array.Copy(buffer, tempep, dst, 0, count);
                            retsize = count;
                            tempep = (tempep + count + capacity) % capacity;
                        }
                        else
                        {
                            int copyleng1 = capacity - tempep;
                            int copyleng2 = count - copyleng1;
                            Array.Copy(buffer, tempep, dst, 0, copyleng1);
                            tempep = (tempep + copyleng1 + capacity) % capacity;
                            retsize = copyleng1;
                            if (copyleng2 >= sp)
                                copyleng2 = sp;
                            Array.Copy(buffer, tempep, dst, copyleng1, copyleng2);
                            retsize += copyleng2;
                            tempep = (tempep + copyleng2 + capacity) % capacity;
                        }
                    }

                    //Debug.WriteLine("L2-" + count.ToString());

                    return retsize;

                    //int size = Math.Min(count, (sp - ep + capacity) % capacity);
                    //if (tempep >= capacity)
                    //    tempep = 0;

                    //if ((tempep + size) >= capacity)
                    //{
                    //    int copyleng1 = capacity - tempep;
                    //    int copyleng2 = size - copyleng1;
                    //    Array.Copy(buffer, tempep, dst, 0, copyleng1);
                    //    tempep += copyleng1;
                    //    if (tempep >= capacity)
                    //        tempep = 0;
                    //    Array.Copy(buffer, tempep, dst, copyleng1, copyleng2);
                    //}
                    //else
                    //{
                    //    Array.Copy(buffer, tempep, dst, 0, size);
                    //}
                    //ITNTTraceLog.Instance.Trace(3, "L2");
                    //return size;
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return ex.HResult;
            }
        }

        public int Look(ref byte value)
        {
            string className = "RingBuffer";
            string funcName = "Look";
            try
            {
                lock (lockobj)
                {
                    if (sp == ep)
                        return 0;
                    if (ep >= capacity)
                        value = buffer[ep % capacity];
                    else
                        value = buffer[ep];
                    //ep = 0;
                    //value = buffer[ep];
                    //if (ep >= MAX_BUFFER_SIZE)
                    //    ep = 0;
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return ex.HResult;
            }
            return 1;
        }

        public int LookReverse(ref byte value)
        {
            try
            {
                lock (lockobj)
                {
                    if (sp == ep)
                        return 0;
                    if (sp <= 0)
                        value = buffer[capacity - 1];
                    else if (sp >= capacity)
                        value = buffer[sp % capacity];
                    else
                        value = buffer[sp - 1];
                }
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }
            return 1;
        }

        public int LookReverse(byte[] dst, int count)
        {
            string className = "RingBuffer";
            string funcName = "LookReverse";
            try
            {
                lock (lockobj)
                {
                    if (ep == sp)
                        return 0;

                    //int tempep = ep;
                    int tempsp = sp;
                    int size = Math.Min(count, (sp - ep + capacity) % capacity);
                    if (size <= 0)
                        return 0;

                    if ((tempsp - size) < 0)
                    {
                        int copyleng1 = tempsp;
                        int copyleng2 = size - copyleng1;
                        int cppt2 = capacity - copyleng2;
                        if (copyleng1 > 0)
                        {
                            Array.Copy(buffer, cppt2, dst, 0, copyleng2);
                            Array.Copy(buffer, 0, dst, copyleng2, copyleng1);
                        }
                    }
                    //else if ((tempsp - size) == 0)
                    //{
                    //    Array.Copy(buffer, tempsp-size, dst, 0, size);
                    //}
                    else
                    {
                        Array.Copy(buffer, tempsp - size, dst, 0, size);
                    }
                    return size;
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return ex.HResult;
            }
        }

        public int GetSize()
        {
            try
            {
                lock (lockobj)
                {
                    if (capacity <= 0)
                        return 0;
                    else
                        return ((sp - ep + capacity) % capacity);
                }
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }
        }




        //public void CopyTo(T[] array)
        //{
        //    CopyTo(array, 0);
        //}

        //public void CopyTo(T[] array, int arrayIndex)
        //{
        //    CopyTo(0, array, arrayIndex, size);
        //}

        //public void CopyTo(int index, T[] array, int arrayIndex, int count)
        //{
        //    if (count > size)
        //        count = size;

        //    //throw new ArgumentOutOfRangeException("count", Properties.Resources.MessageReadCountTooLarge);

        //    int bufferIndex = head;
        //    for (int i = 0; i < count; i++, bufferIndex++, arrayIndex++)
        //    {
        //        if (bufferIndex == capacity)
        //            bufferIndex = 0;
        //        array[arrayIndex] = buffer[bufferIndex];
        //    }
        //}

        //public IEnumerator<T> GetEnumerator()
        //{
        //    int bufferIndex = head;
        //    for (int i = 0; i < size; i++, bufferIndex++)
        //    {
        //        if (bufferIndex == capacity)
        //            bufferIndex = 0;

        //        yield return buffer[bufferIndex];
        //    }
        //}

        //public T[] GetBuffer()
        //{
        //    return buffer;
        //}

        //public T[] ToArray()
        //{
        //    var dst = new T[size];
        //    CopyTo(dst);
        //    return dst;
        //}



        //#region ICollection<T> Members

        //int ICollection<T>.Count
        //{
        //    get { return size; }
        //}

        //bool ICollection<T>.IsReadOnly
        //{
        //    get { return false; }
        //}

        //void ICollection<T>.Add(T item)
        //{
        //    Put(item);
        //}

        //bool ICollection<T>.Remove(T item)
        //{
        //    if (size == 0)
        //        return false;

        //    Get();
        //    return true;
        //}

        //#endregion

        //#region IEnumerable<T> Members

        //IEnumerator<T> IEnumerable<T>.GetEnumerator()
        //{
        //    return GetEnumerator();
        //}

        //#endregion

        //#region ICollection Members

        //int ICollection.Count
        //{
        //    get { return size; }
        //}

        //bool ICollection.IsSynchronized
        //{
        //    get { return false; }
        //}

        //object ICollection.SyncRoot
        //{
        //    get
        //    {
        //        if (syncRoot == null)
        //            Interlocked.CompareExchange(ref syncRoot, new object(), null);
        //        return syncRoot;
        //    }
        //}

        //void ICollection.CopyTo(Array array, int arrayIndex)
        //{
        //    CopyTo((T[])array, arrayIndex);
        //}

        //#endregion

        //#region IEnumerable Members

        //IEnumerator IEnumerable.GetEnumerator()
        //{
        //    return (IEnumerator)GetEnumerator();
        //}

        //#endregion
    }
}
