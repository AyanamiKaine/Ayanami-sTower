using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;

namespace CProjectMakerLogic
{
    public class Request
    {
        public string ProjectName { get; set; } = "";
        public string ProjectPath { get; set; } = "";
        public string CVersion { get; set; } = "";
        public string MainTemplate { get; set; } = "";
        public bool CreateExecutable { get; set; } = false;
        public bool CreateLibrary { get; set; } = false;
        public bool AddVCPKG { get; set; } = false;
        public bool AutoAddFiles { get; set; } = false;
        public bool AddCJson { get; set; } = false;
        public bool AddCzmq { get; set; } = false;
        public bool AddSokol { get; set; } = false;
        public bool AddNuklear { get; set; } = false;
        public bool AddArenaAllocator { get; set; } = false;
        public bool AddCString { get; set; } = false;
        public bool AddFlecs { get; set; } = false;
        public bool AddLuaJIT { get; set; } = false;
        public bool AddNNG { get; set; } = false;
        public string PrettyPrint()
        {
            return $@"
Request Details:
- Project Name: {ProjectName}
- Project Path: {ProjectPath}
- C Version: {CVersion}
- Create Executable: {CreateExecutable}
- Create Library: {CreateLibrary}
- Add VCPKG: {AddVCPKG}
- Auto Add Files: {AutoAddFiles}
- Add CJson: {AddCJson}
- Add Czmq: {AddCzmq}
- Add Sokol: {AddSokol}
- Add Nuklear: {AddNuklear}
- Add Arena Allocator: {AddArenaAllocator}
- Add CString: {AddCString}
- Add NNG: {AddNNG}";
        }
    }
}
