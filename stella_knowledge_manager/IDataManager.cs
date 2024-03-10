using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stella_knowledge_manager
{
    /// <summary>
    /// Defines the interface to save, load, and to create a backup of our files to learn.
    /// </summary>
    public interface IDataManager
    {
        public void SaveData(string filePath, string fileName);
        public void LoadData(string filePath, string fileName);
        
        // Why is a backup interface defined and part of the DataManager Interface ?
        // Because redundancy is a good property of a robust system, when you define the ability to save you want to have
        // the explitit ability to create backups.

        // Why Is there are a CreateBackup interface when its the same as the SavaData? Because the intend is different
        // Backups are something that should be done separetly from saving.

        public void CreateBackup(string filePath, string fileName);
        public void LoadFromBackup(string filePath, string fileName);
    }
}
