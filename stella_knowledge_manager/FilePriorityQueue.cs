using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stella_knowledge_manager
{
    public class FilePriorityQueue : PriorityQueue<string, float>
    {
        
        public override void AddItem(string filePath, float priority = 0.0f)
        {
            _items[filePath] = priority;
        }

        public override void RemoveItem(string filePath)
        {
            _items.Remove(filePath);
        }

        public override string Peek()
        {
            return "";
        }

        public override int Count()
        {
            return _items.Count;
        }

        public override float GetPriority(string filePath)
        {
            return _items[filePath];
        }

        public override void SetPriority(string filePath, float priority)
        {
            _items[filePath] = priority;
        }

        public override void Clear()
        {
            _items.Clear();
        }
    }
}
