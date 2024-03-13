using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stella_knowledge_manager
{
    public class SRSItemFilesDatabase : IDataManager , ISRSDatabase
    {
        private Dictionary<Guid, ISRSItem> database = new Dictionary<Guid, ISRSItem>();

        public void AddItem(ISRSItem item)
        {
            database[item.Id] = item;
        }

        public void UpdateItem(Guid id, ISRSItem updatedItem)
        {
            throw new NotImplementedException();
        }

        public void DeleteItem(Guid id)
        {
            throw new NotImplementedException();
        }

        public ISRSItem GetItem(Guid id)
        {
            return database[id];
        }

        public ISRSItem GetItem(string id)
        {
            return database[new Guid(id)];
        }

        public void LoadData(string filePath = "Stella Knowledge Manager", string fileName = "main_save_data.json")
        {
            throw new NotImplementedException();
        }
        
        public void SaveData(string filePath = "Stella Knowledge Manager", string fileName = "main_save_data.json")
        {
            throw new NotImplementedException();
        }

        public void LoadFromBackup(string filePath = "Stella Knowledge Manager/backups", string fileName = "backup_save_data")
        {
            throw new NotImplementedException();
        }

        public void CreateBackup(string filePath = "Stella Knowledge Manager/backups", string fileName = "backup_save_data")
        {
            throw new NotImplementedException();
        }

    }
}
