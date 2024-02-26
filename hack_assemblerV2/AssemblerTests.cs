using HackAssemblerV2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace HackAssemblerV2
{
    public class AssemblerTests
    {

        //Every output binary of a C-Instrution (dest=comp;jump) as the binary structure of
        //111 a comp dest jump

        // Assemble Tests are simple tests where we compare our generated binaries with correct ones.
        [Fact]
        public void Assembler_Test_1()
        {
            Assembler assembler = new("A=D+A");

            List<string> binaryOutput = assembler.Assemble();

            Assert.Equal("1110000010100000" , binaryOutput[0]);
        }

        [Fact]
        public void Assembler_Test_2()
        {
            Assembler assembler = new("@SCREEN");

            List<string> binaryOutput = assembler.Assemble();

            Assert.Equal("0100000000000000", binaryOutput[0]);
        }

        [Fact]
        public void Assembler_Test_3()
        {
            Assembler assembler = new("D;JLE");

            List<string> binaryOutput = assembler.Assemble();

            Assert.Equal("1110001100000110", binaryOutput[0]);
        }


        [Fact]
        public void Assembler_Test_4()
        {
            Assembler assembler = new("D;JLE \n //Comment \n A=A+1");

            List<string> binaryOutput = assembler.Assemble();

            Assert.Equal("1110001100000110", binaryOutput[0]);
        }

        [Fact]
        public void Assembler_Test_5()
        {
            Assembler assembler = new("D;JLE \n //Comment \n A=A+1");

            List<string> binaryOutput = assembler.Assemble();

            Assert.Equal("1110110111100000", binaryOutput[1]);
        }

        [Fact]
        public void Assembler_Test_6()
        {
            Assembler assembler = new("D;JLE \n (SYMBOL_X) \n @SYMBOL_X");

            List<string> binaryOutput = assembler.Assemble();

            Assert.Equal("0000000000000001", binaryOutput[1]);
        }

        [Fact]
        public void Assembler_Test_7()
        {
            Assembler assembler = new("D;JLE \n @TEST \n @SYMBOL_X");

            List<string> binaryOutput = assembler.Assemble();

            Assert.Equal("0000000000010000", binaryOutput[1]);
        }


        [Fact]
        public void Assembler_Test_8()
        {
            Assembler assembler = new("D;JLE \n @TEST \n @SYMBOL_X");

            List<string> binaryOutput = assembler.Assemble();

            Assert.Equal("0000000000010001", binaryOutput[2]);
        }

        [Fact]
        public void Assembler_Test_9()
        {
            Assembler assembler = new("@RET_ADDRESS_CALL0\r\nD=A\r\n@95\r\n0;JMP\r\n(RET_ADDRESS_CALL0)");

            List<string> binaryOutput = assembler.Assemble();

            Assert.Equal("0000000000000100", binaryOutput[0]);
        }

        [Fact]
        public void Assembler_Test_10()
        {
            Assembler assembler = new("@RET_ADDRESS_CALL0\r\nD=A\r\n@95\r\n0;JMP\r\n(RET_ADDRESS_CALL0)");

            List<string> binaryOutput = assembler.Assemble();

            Assert.Equal("0000000001011111", binaryOutput[2]);
        }

        [Fact]
        public void Assembler_Integration_Test_1()
        {
            StreamReader streamReader = new("./example_asm_files/add/Add.asm");

            string input = streamReader.ReadToEnd();

            Assembler assembler = new(input);


            List<string> output = assembler.Assemble();

            StreamWriter streamWriter = new("./output/add.hack");
            foreach (var line in output)
            {
                streamWriter.WriteLine(line);
            }
            streamWriter.Close();
        }


        [Fact]
        public void Assembler_Integration_Test_2()
        {
            StreamReader streamReader = new("./example_asm_files/max/Max.asm");

            string input = streamReader.ReadToEnd();

            Assembler assembler = new(input);


            List<string> output = assembler.Assemble();

            StreamWriter streamWriter = new("./output/max.hack");
            foreach (var line in output)
            {
                streamWriter.WriteLine(line);
            }
            streamWriter.Close();
        }

        [Fact]
        public void Assembler_Integration_Test_3()
        {
            StreamReader streamReader = new("./example_asm_files/pong/Pong.asm");

            string input = streamReader.ReadToEnd();

            Assembler assembler = new(input);


            List<string> output = assembler.Assemble();

            StreamWriter streamWriter = new("./output/pong.hack");
            foreach (var line in output)
            {
                streamWriter.WriteLine(line);
            }
            streamWriter.Close();
        }

        [Fact]
        public void Assembler_Integration_Test_4()
        {
            StreamReader streamReader = new("./example_asm_files/rect/Rect.asm");

            string input = streamReader.ReadToEnd();

            Assembler assembler = new(input);


            List<string> output = assembler.Assemble();

            StreamWriter streamWriter = new("./output/rect.hack");
            foreach (var line in output)
            {
                streamWriter.WriteLine(line);
            }
            streamWriter.Close();
        }

        // If this tests complete we can confidently say that our assembler is functional, correct, and works on any correct
        // asm. files for the Hack-16-Bit CPU
        [Fact]
        public void Assembler_CompareGeneratedBinaryToCorrectOne_Test_1()
        {
            StreamReader streamReader = new("./example_asm_files/pong/Pong.asm");

            string input = streamReader.ReadToEnd();

            Assembler assembler = new(input);


            List<string> output = assembler.Assemble();

            StreamWriter streamWriter = new("./output/pong.hack");
            foreach (var line in output)
            {
                streamWriter.WriteLine(line);
            }
            streamWriter.Close();

            IEnumerable<string> file1Lines = File.ReadLines("./output/pong.hack");
            IEnumerable<string> file2Lines = File.ReadLines("./correctBinary/pong.hack");

            var file1ExclusiveLines = file1Lines.Except(file2Lines).ToList();
            var file2ExclusiveLines = file2Lines.Except(file1Lines).ToList();

            Console.WriteLine("Lines only in file 1:");
            foreach (var line in file1ExclusiveLines)
            {
                Console.WriteLine(line);
            }

            Console.WriteLine("Lines only in file 2:");
            foreach (var line in file2ExclusiveLines)
            {
                Console.WriteLine(line);
            }

            Assert.True(file1ExclusiveLines.Count == 0 || file2ExclusiveLines.Count == 0);
        }
    }
}
