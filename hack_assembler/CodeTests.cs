using HackAssemblerV2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace HackAssemblerV2
{
    public class CodeTests
    {
        [Fact]
        public void dest_returns_binary_code_of_dest_mnemonic_UnitTest1()
        {
            Code code = new();

            string output = code.Dest("null");

            Assert.Equal("000", output);
        }

        [Fact]
        public void dest_returns_binary_code_of_dest_mnemonic_UnitTest2()
        {
            Code code = new();

            string output = code.Dest("M");

            Assert.Equal("001", output);
        }

        [Fact]
        public void dest_returns_binary_code_of_dest_mnemonic_UnitTest3()
        {
            Code code = new();

            string output = code.Dest("D");

            Assert.Equal("010", output);
        }

        [Fact]
        public void dest_returns_binary_code_of_dest_mnemonic_UnitTest4()
        {
            Code code = new();

            string output = code.Dest("MD");

            Assert.Equal("011", output);
        }

        [Fact]
        public void dest_returns_binary_code_of_dest_mnemonic_UnitTest5()
        {
            Code code = new();

            string output = code.Dest("A");

            Assert.Equal("100", output);
        }

        [Fact]
        public void dest_returns_binary_code_of_dest_mnemonic_UnitTest6()
        {
            Code code = new();

            string output = code.Dest("AM");

            Assert.Equal("101", output);
        }


        [Fact]
        public void dest_returns_binary_code_of_dest_mnemonic_UnitTest7()
        {
            Code code = new();

            string output = code.Dest("AD");

            Assert.Equal("110", output);
        }

        [Fact]
        public void dest_returns_binary_code_of_dest_mnemonic_UnitTest8()
        {
            Code code = new();

            string output = code.Dest("ADM");

            Assert.Equal("111", output);
        }

        [Fact]
        public void comp_returns_binary_code_of_comp_mnemonic_UnitTest1()
        {
            Code code = new();

            string output = code.Comp("0");

            Assert.Equal("0101010", output);

        }

        [Fact]
        public void comp_returns_binary_code_of_comp_mnemonic_UnitTest2()
        {
            Code code = new();

            string output = code.Comp("1");

            Assert.Equal("0111111", output);
        }

        [Fact]
        public void comp_returns_binary_code_of_comp_mnemonic_UnitTest3()
        {
            Code code = new();

            string output = code.Comp("-1");

            Assert.Equal("0111010", output);

        }

        [Fact]
        public void comp_returns_binary_code_of_comp_mnemonic_UnitTest4()
        {
            Code code = new();

            string output = code.Comp("D");

            Assert.Equal("0001100", output);

        }

        [Fact]
        public void comp_returns_binary_code_of_comp_mnemonic_UnitTest5()
        {
            Code code = new();

            string output = code.Comp("A");

            Assert.Equal("0110000", output);

        }

        [Fact]
        public void comp_returns_binary_code_of_comp_mnemonic_UnitTest6()
        {
            Code code = new();

            string output = code.Comp("!D");

            Assert.Equal("0001101", output);

        }

        [Fact]
        public void comp_returns_binary_code_of_comp_mnemonic_UnitTest7()
        {
            Code code = new();

            string output = code.Comp("!A");

            Assert.Equal("0110001", output);

        }

        [Fact]
        public void comp_returns_binary_code_of_comp_mnemonic_UnitTest8()
        {
            Code code = new();

            string output = code.Comp("-D");

            Assert.Equal("0001111", output);

        }

        [Fact]
        public void comp_returns_binary_code_of_comp_mnemonic_UnitTest9()
        {
            Code code = new();

            string output = code.Comp("-D");

            Assert.Equal("0001111", output);

        }

        [Fact]
        public void comp_returns_binary_code_of_comp_mnemonic_UnitTest10()
        {
            Code code = new();

            string output = code.Comp("D+1");

            Assert.Equal("0011111", output);

        }

        [Fact]
        public void comp_returns_binary_code_of_comp_mnemonic_UnitTest11()
        {
            Code code = new();

            string output = code.Comp("A-1");

            Assert.Equal("0110010", output);

        }

        [Fact]
        public void comp_returns_binary_code_of_comp_mnemonic_UnitTest12()
        {
            Code code = new();

            string output = code.Comp("D+A");

            Assert.Equal("0000010", output);

        }

        [Fact]
        public void comp_returns_binary_code_of_comp_mnemonic_UnitTest13()
        {
            Code code = new();

            string output = code.Comp("D-A");

            Assert.Equal("0010011", output);

        }

        [Fact]
        public void comp_returns_binary_code_of_comp_mnemonic_UnitTest14()
        {
            Code code = new();

            string output = code.Comp("A-D");

            Assert.Equal("0000111", output);

        }

        [Fact]
        public void comp_returns_binary_code_of_comp_mnemonic_UnitTest15()
        {
            Code code = new();

            string output = code.Comp("D&A");

            Assert.Equal("0000000", output);

        }

        [Fact]
        public void comp_returns_binary_code_of_comp_mnemonic_UnitTest16()
        {
            Code code = new();

            string output = code.Comp("D|A");

            Assert.Equal("0010101", output);

        }

        [Fact]
        public void comp_returns_binary_code_of_comp_mnemonic_UnitTest17()
        {
            Code code = new();

            string output = code.Comp("M");

            Assert.Equal("1110000", output);

        }

        [Fact]
        public void comp_returns_binary_code_of_comp_mnemonic_UnitTest18()
        {
            Code code = new();

            string output = code.Comp("!M");

            Assert.Equal("1110001", output);

        }

        [Fact]
        public void comp_returns_binary_code_of_comp_mnemonic_UnitTest19()
        {
            Code code = new();

            string output = code.Comp("-M");

            Assert.Equal("1110011", output);

        }

        [Fact]
        public void comp_returns_binary_code_of_comp_mnemonic_UnitTest20()
        {
            Code code = new();

            string output = code.Comp("M+1");

            Assert.Equal("1110111", output);

        }


        [Fact]
        public void comp_returns_binary_code_of_comp_mnemonic_UnitTest21()
        {
            Code code = new();

            string output = code.Comp("M-1");

            Assert.Equal("1110010", output);

        }


        [Fact]
        public void comp_returns_binary_code_of_comp_mnemonic_UnitTest22()
        {
            Code code = new();

            string output = code.Comp("D+M");

            Assert.Equal("1000010", output);

        }

        [Fact]
        public void comp_returns_binary_code_of_comp_mnemonic_UnitTest23()
        {
            Code code = new();

            string output = code.Comp("D-M");

            Assert.Equal("1010011", output);

        }

        [Fact]
        public void comp_returns_binary_code_of_comp_mnemonic_UnitTest24()
        {
            Code code = new();

            string output = code.Comp("M-D");

            Assert.Equal("1000111", output);

        }

        [Fact]
        public void comp_returns_binary_code_of_comp_mnemonic_UnitTest25()
        {
            Code code = new();

            string output = code.Comp("D&M");

            Assert.Equal("1000000", output);

        }

        [Fact]
        public void comp_returns_binary_code_of_comp_mnemonic_UnitTest26()
        {
            Code code = new();

            string output = code.Comp("D|M");

            Assert.Equal("1010101", output);

        }

        [Fact]
        public void jump_returns_binary_code_of_jump_mnemonic_UnitTest1()
        {
            Code code = new();

            string output = code.Jump("null");

            Assert.Equal("000", output);
        }

        [Fact]
        public void jump_returns_binary_code_of_jump_mnemonic_UnitTest2()
        {
            Code code = new();

            string output = code.Jump("JGT");

            Assert.Equal("001", output);
        }


        [Fact]
        public void jump_returns_binary_code_of_jump_mnemonic_UnitTest3()
        {
            Code code = new();

            string output = code.Jump("JEQ");

            Assert.Equal("010", output);
        }

        [Fact]
        public void jump_returns_binary_code_of_jump_mnemonic_UnitTest4()
        {
            Code code = new();

            string output = code.Jump("JGE");

            Assert.Equal("011", output);
        }

        [Fact]
        public void jump_returns_binary_code_of_jump_mnemonic_UnitTest5()
        {
            Code code = new();

            string output = code.Jump("JLT");

            Assert.Equal("100", output);
        }

        [Fact]
        public void jump_returns_binary_code_of_jump_mnemonic_UnitTest6()
        {
            Code code = new();

            string output = code.Jump("JNE");

            Assert.Equal("101", output);
        }

        [Fact]
        public void jump_returns_binary_code_of_jump_mnemonic_UnitTest7()
        {
            Code code = new();

            string output = code.Jump("JLE");

            Assert.Equal("110", output);
        }


        [Fact]
        public void jump_returns_binary_code_of_jump_mnemonic_UnitTest8()
        {
            Code code = new();

            string output = code.Jump("JMP");

            Assert.Equal("111", output);
        }
    }
}
