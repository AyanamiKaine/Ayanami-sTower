using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace stella_knowledge_manager
{
    public class SRSItemFilesDatabaseUnitTests
    {
        [Fact]
        public void AddItemToDatabaseUnitTest()
        {
            SRSItemFilesDatabase database = new SRSItemFilesDatabase();

            FileToLearn itemToStore = new(Guid.NewGuid(), "TESTITEM", "", "Test", 0, 0);

            database.AddItem(itemToStore);
            ISRSItem storedItem = database.GetItem(itemToStore.Id);

            Assert.Equal(itemToStore.Id, storedItem.Id);
        }

        [Fact]
        public void RemoveItemFromDatabaseUnitTest()
        {
            SRSItemFilesDatabase database = new SRSItemFilesDatabase();
            FileToLearn itemToStore = new(Guid.NewGuid(), "TESTITEM", "", "Test", 0, 0);

            database.AddItem(itemToStore);
            database.DeleteItem(itemToStore.Id);
            ISRSItem storedItem = database.GetItem(itemToStore.Id);

        }

        [Fact]
        public void GetItemByIDFromDatabaseUnitTest() 
        {
            SRSItemFilesDatabase database = new SRSItemFilesDatabase();
            database.GetItem(Guid.NewGuid());
        }

        [Fact]
        public void GetItemByNameFromDatabaseUnitTest()
        {
            SRSItemFilesDatabase database = new SRSItemFilesDatabase();
            database.GetItem("Test");
        }

        [Fact]
        public void SaveDatabaseUnitTest()
        {
            SRSItemFilesDatabase database = new SRSItemFilesDatabase();
            database.SaveData("Stella Knowledge Manager", "main_save_data.json");
        }

        [Fact]
        public void LoadDatabaseSaveUnitTest()
        {
            SRSItemFilesDatabase database = new SRSItemFilesDatabase();
            database.LoadData();
        }
    }
}
