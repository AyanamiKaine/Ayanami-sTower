using HackAssemblerV2;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace HackAssemblerV2
{
    public class TokenizerUnitTests
    {
        [Fact]
        public void Tokenizer_Comment_Test_1()
        {
            Tokenizer tokenizer = new("//D=D+M");

            List<Token> currentTokens = tokenizer.ScanTokens();

            Assert.Equal("" , currentTokens[0].Value);
        }

        [Fact]
        public void Tokenizer_Comment_Test_2()
        {
            Tokenizer tokenizer = new("//D=D+M");

            List<Token> currentTokens = tokenizer.ScanTokens();

            Assert.Equal(TokenType.EOF, currentTokens[0].Type);
        }

        [Fact]
        public void Tokenizer_Instruction_Test_1()
        {
            Tokenizer tokenizer = new("D=D+M");

            List<Token> currentTokens = tokenizer.ScanTokens();

            Assert.Equal(TokenType.DEST, currentTokens[0].Type);
        }


        [Fact]
        public void Tokenizer_Instruction_Test_2()
        {
            Tokenizer tokenizer = new("D=D+M");

            List<Token> currentTokens = tokenizer.ScanTokens();

            Assert.Equal(TokenType.COMP, currentTokens[1].Type);
        }

        [Fact]
        public void Tokenizer_Instruction_Test_3()
        {
            Tokenizer tokenizer = new("D=D+M");

            List<Token> currentTokens = tokenizer.ScanTokens();

            Assert.Equal("D+M", currentTokens[1].Value);
        }

        [Fact]
        public void Tokenizer_Symbol_Test_1()
        {
            Tokenizer tokenizer = new("@423");

            List<Token> currentTokens = tokenizer.ScanTokens();

            Assert.Equal("@423", currentTokens[0].Value);
        }

        [Fact]
        public void Tokenizer_Symbol_Test_2()
        {
            Tokenizer tokenizer = new("@423");

            List<Token> currentTokens = tokenizer.ScanTokens();

            Assert.Equal(TokenType.SYMBOL , currentTokens[0].Type);
        }

        [Fact]
        public void Tokenizer_Label_Test_1()
        {
            Tokenizer tokenizer = new("(END)");

            List<Token> currentTokens = tokenizer.ScanTokens();

            Assert.Equal("(END)", currentTokens[0].Value);
        }

        [Fact]
        public void Tokenizer_Label_Test_2()
        {
            Tokenizer tokenizer = new("(END)");

            List<Token> currentTokens = tokenizer.ScanTokens();

            Assert.Equal(TokenType.LABEL, currentTokens[0].Type);
        }

        [Fact]
        public void Tokenizer_Dest_Test_1()
        {
            Tokenizer tokenizer = new("D=D+A");

            List<Token> currentTokens = tokenizer.ScanTokens();

            Assert.Equal(TokenType.DEST, currentTokens[0].Type);
        }

        [Fact]
        public void Tokenizer_Dest_Test_2()
        {
            Tokenizer tokenizer = new("D=D+A");

            List<Token> currentTokens = tokenizer.ScanTokens();

            Assert.Equal("D", currentTokens[0].Value);
        }

        [Fact]
        public void Tokenizer_Dest_Test_3()
        {
            Tokenizer tokenizer = new("ADM=D+A");

            List<Token> currentTokens = tokenizer.ScanTokens();

            Assert.Equal("ADM", currentTokens[0].Value);
        }

        [Fact]
        public void Tokenizer_Dest_Test_4()
        {
            Tokenizer tokenizer = new("DM=D+A");

            List<Token> currentTokens = tokenizer.ScanTokens();

            Assert.Equal("DM", currentTokens[0].Value);
        }

        [Fact]
        public void Tokenizer_Dest_Test_5()
        {
            Tokenizer tokenizer = new("(SCREEN) \n @13 \n A=A+1 \n DM=D+A");

            List<Token> currentTokens = tokenizer.ScanTokens();

            Assert.Equal("A", currentTokens[2].Value);
        }

        [Fact]
        public void Tokenizer_Dest_Test_6()
        {
            Tokenizer tokenizer = new("//Hello World does this work? \n DM=D+A");

            List<Token> currentTokens = tokenizer.ScanTokens();

            Assert.Equal("DM", currentTokens[0].Value);
        }

        [Fact]
        public void Tokenizer_Dest_Test_7()
        {
            Tokenizer tokenizer = new("//Hello World does this work? \n DM=D+A \n A=D-A");

            List<Token> currentTokens = tokenizer.ScanTokens();

            Assert.Equal("A", currentTokens[3].Value);
        }

        [Fact]
        public void Tokenizer_Dest_Test_8()
        {
            Tokenizer tokenizer = new("//Hello World does this work? \n DM=D+A \n A=D-A");

            List<Token> currentTokens = tokenizer.ScanTokens();

            Assert.Equal(TokenType.DEST, currentTokens[3].Type);
        }

        [Fact]
        public void Tokenizer_Dest_Test_9()
        {
            Tokenizer tokenizer = new("//Hello World does this work? \n 0;JMP \n A=D-A");

            List<Token> currentTokens = tokenizer.ScanTokens();

            Assert.Equal("", currentTokens[0].Value);
        }

        [Fact]
        public void Tokenizer_Dest_Test_10()
        {
            Tokenizer tokenizer = new("//Hello World does this work? \n 0;JMP \n A=D-A");

            List<Token> currentTokens = tokenizer.ScanTokens();

            Assert.Equal(TokenType.DEST, currentTokens[0].Type);
        }

        [Fact]
        public void Tokenizer_Comp_Test_1()
        {
            Tokenizer tokenizer = new("//Hello World does this work? \n DM=D+A \n A=D-A");

            List<Token> currentTokens = tokenizer.ScanTokens();

            Assert.Equal("D-A", currentTokens[4].Value);
        }

        [Fact]
        public void Tokenizer_Comp_Test_2()
        {
            Tokenizer tokenizer = new("//Hello World does this work? \n DM=D+A \n A=D-A");

            List<Token> currentTokens = tokenizer.ScanTokens();

            Assert.Equal(TokenType.COMP, currentTokens[4].Type);
        }

        [Fact]
        public void Tokenizer_Comp_Test_3()
        {
            Tokenizer tokenizer = new("//Hello World does this work? \n DM=D+A \n A=D-A");

            List<Token> currentTokens = tokenizer.ScanTokens();

            Assert.Equal(TokenType.COMP, currentTokens[4].Type);
        }

        [Fact]
        public void Tokenizer_Comp_Test_4()
        {
            Tokenizer tokenizer = new("//Hello World does this work? \n DM=D+A \n A=D-A");

            List<Token> currentTokens = tokenizer.ScanTokens();

            Assert.Equal("D+A", currentTokens[1].Value);
        }

        [Fact]
        public void Tokenizer_Comp_Test_5()
        {
            Tokenizer tokenizer = new("//Hello World does this work? \n DM=D+A \n A=D-A");

            List<Token> currentTokens = tokenizer.ScanTokens();

            Assert.Equal("D-A", currentTokens[4].Value);
        }

        [Fact]
        public void Tokenizer_Comp_Test_6()
        {
            Tokenizer tokenizer = new("//Hello World does this work? \n DM=D+A \n A=!A");

            List<Token> currentTokens = tokenizer.ScanTokens();

            Assert.Equal("!A", currentTokens[4].Value);
        }

        [Fact]
        public void Tokenizer_Comp_Test_7()
        {
            Tokenizer tokenizer = new("//Hello World does this work? \n DM=D+A \n A=-1");

            List<Token> currentTokens = tokenizer.ScanTokens();

            Assert.Equal("-1", currentTokens[4].Value);
        }

        [Fact]
        public void Tokenizer_Comp_Test_8()
        {
            Tokenizer tokenizer = new("//Hello World does this work? \n DM=D+A \n A=D|A");

            List<Token> currentTokens = tokenizer.ScanTokens();

            Assert.Equal("D|A", currentTokens[4].Value);
        }

        [Fact]
        public void Tokenizer_Comp_Test_9()
        {
            Tokenizer tokenizer = new("//Hello World does this work? \n DM=D+A \n A=M-1");

            List<Token> currentTokens = tokenizer.ScanTokens();

            Assert.Equal("M-1", currentTokens[4].Value);
        }


        [Fact]
        public void Tokenizer_Jump_Test_1()
        {
            Tokenizer tokenizer = new("//Hello World does this work? \n DM=D+A \n A=D-A");

            List<Token> currentTokens = tokenizer.ScanTokens();

            Assert.Equal("", currentTokens[2].Value);
        }

        [Fact]
        public void Tokenizer_Jump_Test_2()
        {
            Tokenizer tokenizer = new("//Hello World does this work? \n 0;JGE \n A=D-A");

            List<Token> currentTokens = tokenizer.ScanTokens();

            Assert.Equal(TokenType.JUMP, currentTokens[2].Type);
        }

        [Fact]
        public void Tokenizer_Jump_Test_3()
        {
            Tokenizer tokenizer = new("//Hello World does this work? \n 0;JGE \n A=D-A");

            List<Token> currentTokens = tokenizer.ScanTokens();

            Assert.Equal("JGE", currentTokens[2].Value);
        }

        [Fact]
        public void Tokenizer_Jump_Test_4()
        {
            Tokenizer tokenizer = new("//Hello World does this work? \n 0;JGE \n A=D-A;JMP");

            List<Token> currentTokens = tokenizer.ScanTokens();

            Assert.Equal("JMP", currentTokens[5].Value);
        }

        [Fact]
        public void Tokenizer_Jump_Test_5()
        {
            Tokenizer tokenizer = new("//Hello World does this work? \n 0;JGE \n A=D-A;JMP");

            List<Token> currentTokens = tokenizer.ScanTokens();

            Assert.Equal(TokenType.JUMP, currentTokens[5].Type);
        }

        [Fact]
        public void Tokenizer_Jump_Test_6()
        {
            Tokenizer tokenizer = new("//Hello World does this work? \n 0;JGE \n A=D-A;JMP \n (SCREEN) \n @23 \n 0;JLT ");

            List<Token> currentTokens = tokenizer.ScanTokens();

            Assert.Equal("JLT", currentTokens[10].Value);
        }

        [Fact]
        public void Tokenizer_Jump_Test_7()
        {
            Tokenizer tokenizer = new("//Hello World does this work? \n 0;JGE \n A=D-A;JMP \n (SCREEN) \n @23 \n 0;JLT ");

            List<Token> currentTokens = tokenizer.ScanTokens();

            Assert.Equal(TokenType.JUMP, currentTokens[10].Type);
        }

        [Fact]
        public void Tokenizer_Jump_Test_8()
        {
            Tokenizer tokenizer = new("//Hello World does this work? \n 0;JGE \n A=D-A;JMP \n (SCREEN) \n @23 \n 0;JNE ");

            List<Token> currentTokens = tokenizer.ScanTokens();

            Assert.Equal(TokenType.JUMP, currentTokens[10].Type);
        }

        [Fact]
        public void Tokenizer_Jump_Test_9()
        {
            Tokenizer tokenizer = new("//Hello World does this work? \n 0;JGE \n A=D-A;JMP \n (SCREEN) \n @23 \n 0;JNE ");

            List<Token> currentTokens = tokenizer.ScanTokens();

            Assert.Equal("JNE", currentTokens[10].Value);
        }

        [Fact]
        public void Tokenizer_Jump_Test_10()
        {
            Tokenizer tokenizer = new("//Hello World does this work? \n 0;JGE \n A=D-A;JMP \n (SCREEN) \n @23 \n 0;JEQ ");

            List<Token> currentTokens = tokenizer.ScanTokens();

            Assert.Equal("JEQ", currentTokens[10].Value);
        }

        [Fact]
        public void Tokenizer_Jump_Test_11()
        {
            Tokenizer tokenizer = new("//Hello World does this work? \n 0;JGE \n A=D-A;JMP \n (SCREEN) \n @23 \n 0;JLT ");

            List<Token> currentTokens = tokenizer.ScanTokens();

            Assert.Equal("JLT", currentTokens[10].Value);
        }

        [Fact]
        public void Tokenizer_Jump_Test_12()
        {
            Tokenizer tokenizer = new("//Hello World does this work? \n 0;JGE \n A=D-A;JMP \n (SCREEN) \n @23 \n 0;JLT ");

            List<Token> currentTokens = tokenizer.ScanTokens();

            Assert.Equal("", currentTokens[8].Value);
        }

        [Fact]
        public void Tokenizer_Jump_Test_13()
        {
            Tokenizer tokenizer = new("//Hello World does this work? \n 0;JGE \n A=D-A;JMP \n (SCREEN) \n @23 \n 0;JLT ");

            List<Token> currentTokens = tokenizer.ScanTokens();

            Assert.Equal(TokenType.DEST, currentTokens[8].Type);
        }

        [Fact]
        public void Tokenizer_Integration_Test_1()
        {
            Tokenizer tokenizer = new("@256\r\nD=A\r\n@SP\r\nM=D\r\n@133\r\n0;JMP\r\n@R15\r\nM=D\r\n@SP\r\nAM=M-1\r\nD=M\r\nA=A-1\r\nD=M-D\r\nM=0\r\n@END_EQ\r\nD;JNE\r\n@SP\r\nA=M-1\r\nM=-1\r\n(END_EQ)\r\n@R15\r\nA=M\r\n0;JMP\r\n@R15\r\nM=D\r\n@SP\r\nAM=M-1\r\nD=M\r\nA=A-1\r\nD=M-D\r\nM=0\r\n@END_GT\r\nD;JLE\r\n@SP\r\nA=M-1\r\nM=-1\r\n(END_GT)");

            List<Token> currentTokens = tokenizer.ScanTokens();

            Assert.Equal(TokenType.SYMBOL, currentTokens[0].Type);
            Assert.Equal("@256", currentTokens[0].Value);
        }

        [Fact]
        public void Tokenizer_Integration_Test_2()
        {
            Tokenizer tokenizer = new("@256\r\nD=A\r\n@SP\r\nM=D\r\n@133\r\n0;JMP\r\n@R15\r\nM=D\r\n@SP\r\nAM=M-1\r\nD=M\r\nA=A-1\r\nD=M-D\r\nM=0\r\n@END_EQ\r\nD;JNE\r\n@SP\r\nA=M-1\r\nM=-1\r\n(END_EQ)\r\n@R15\r\nA=M\r\n0;JMP\r\n@R15\r\nM=D\r\n@SP\r\nAM=M-1\r\nD=M\r\nA=A-1\r\nD=M-D\r\nM=0\r\n@END_GT\r\nD;JLE\r\n@SP\r\nA=M-1\r\nM=-1\r\n(END_GT)");

            List<Token> currentTokens = tokenizer.ScanTokens();

            Assert.Equal(TokenType.DEST, currentTokens[1].Type);
            Assert.Equal("D", currentTokens[1].Value);
        }

        [Fact]
        public void Tokenizer_Integration_Test_3()
        {
            Tokenizer tokenizer = new("@256\r\nD=A\r\n@SP\r\nM=D\r\n@133\r\n0;JMP\r\n@R15\r\nM=D\r\n@SP\r\nAM=M-1\r\nD=M\r\nA=A-1\r\nD=M-D\r\nM=0\r\n@END_EQ\r\nD;JNE\r\n@SP\r\nA=M-1\r\nM=-1\r\n(END_EQ)\r\n@R15\r\nA=M\r\n0;JMP\r\n@R15\r\nM=D\r\n@SP\r\nAM=M-1\r\nD=M\r\nA=A-1\r\nD=M-D\r\nM=0\r\n@END_GT\r\nD;JLE\r\n@SP\r\nA=M-1\r\nM=-1\r\n(END_GT)");

            List<Token> currentTokens = tokenizer.ScanTokens();

            Assert.Equal(TokenType.COMP, currentTokens[2].Type);
            Assert.Equal("A", currentTokens[2].Value);
        }

        [Fact]
        public void Tokenizer_Integration_Test_4()
        {
            Tokenizer tokenizer = new("@256\r\nD=A\r\n@SP\r\nM=D\r\n@133\r\n0;JMP\r\n@R15\r\nM=D\r\n@SP\r\nAM=M-1\r\nD=M\r\nA=A-1\r\nD=M-D\r\nM=0\r\n@END_EQ\r\nD;JNE\r\n@SP\r\nA=M-1\r\nM=-1\r\n(END_EQ)\r\n@R15\r\nA=M\r\n0;JMP\r\n@R15\r\nM=D\r\n@SP\r\nAM=M-1\r\nD=M\r\nA=A-1\r\nD=M-D\r\nM=0\r\n@END_GT\r\nD;JLE\r\n@SP\r\nA=M-1\r\nM=-1\r\n(END_GT)");

            List<Token> currentTokens = tokenizer.ScanTokens();

            Assert.Equal(TokenType.JUMP, currentTokens[3].Type);
            Assert.Equal("", currentTokens[3].Value);
        }

        [Fact]
        public void Tokenizer_Integration_Test_5()
        {
            Tokenizer tokenizer = new("@256\r\nD=A\r\n@SP\r\nM=D\r\n@133\r\n0;JMP\r\n@R15\r\nM=D\r\n@SP\r\nAM=M-1\r\nD=M\r\nA=A-1\r\nD=M-D\r\nM=0\r\n@END_EQ\r\nD;JNE\r\n@SP\r\nA=M-1\r\nM=-1\r\n(END_EQ)\r\n@R15\r\nA=M\r\n0;JMP\r\n@R15\r\nM=D\r\n@SP\r\nAM=M-1\r\nD=M\r\nA=A-1\r\nD=M-D\r\nM=0\r\n@END_GT\r\nD;JLE\r\n@SP\r\nA=M-1\r\nM=-1\r\n(END_GT)");

            List<Token> currentTokens = tokenizer.ScanTokens();

            Assert.Equal(TokenType.SYMBOL, currentTokens[4].Type);
            Assert.Equal("@SP", currentTokens[4].Value);
        }

        [Fact]
        public void Tokenizer_Integration_Test_6()
        {
            Tokenizer tokenizer = new("@256\r\nD=A\r\n@SP\r\nM=D\r\n@133\r\n0;JMP\r\n@R15\r\nM=D\r\n@SP\r\nAM=M-1\r\nD=M\r\nA=A-1\r\nD=M-D\r\nM=0\r\n@END_EQ\r\nD;JNE\r\n@SP\r\nA=M-1\r\nM=-1\r\n(END_EQ)\r\n@R15\r\nA=M\r\n0;JMP\r\n@R15\r\nM=D\r\n@SP\r\nAM=M-1\r\nD=M\r\nA=A-1\r\nD=M-D\r\nM=0\r\n@END_GT\r\nD;JLE\r\n@SP\r\nA=M-1\r\nM=-1\r\n(END_GT)");

            List<Token> currentTokens = tokenizer.ScanTokens();

            Assert.Equal(TokenType.LABEL, currentTokens[43].Type);
        }

        [Fact]
        public void Tokenizer_Integration_Test_7()
        {
            Tokenizer tokenizer = new("@256\r\nD=A\r\n@SP\r\nM=D\r\n@133\r\n0;JMP\r\n@R15\r\nM=D\r\n@SP\r\nAM=M-1\r\nD=M\r\nA=A-1\r\nD=M-D\r\nM=0\r\n@END_EQ\r\nD;JNE\r\n@SP\r\nA=M-1\r\nM=-1\r\n(END_EQ)\r\n@R15\r\nA=M\r\n0;JMP\r\n@R15\r\nM=D\r\n@SP\r\nAM=M-1\r\nD=M\r\nA=A-1\r\nD=M-D\r\nM=0\r\n@END_GT\r\nD;JLE\r\n@SP\r\nA=M-1\r\nM=-1\r\n(END_GT)");

            List<Token> currentTokens = tokenizer.ScanTokens();

            Assert.Equal("(END_EQ)", currentTokens[43].Value);
        }

        [Fact]
        public void Tokenizer_Integration_Test_8()
        {
            Tokenizer tokenizer = new("@256\r\nD=A\r\n@SP\r\nM=D\r\n@133\r\n0;JMP\r\n@R15\r\nM=D\r\n@SP\r\nAM=M-1\r\nD=M\r\nA=A-1\r\nD=M-D\r\nM=0\r\n@END_EQ\r\nD;JNE\r\n@SP\r\nA=M-1\r\nM=-1\r\n(END_EQ)\r\n@R15\r\nA=M\r\n0;JMP\r\n@R15\r\nM=D\r\n@SP\r\nAM=M-1\r\nD=M\r\nA=A-1\r\nD=M-D\r\nM=0\r\n@END_GT\r\nD;JLE\r\n@SP\r\nA=M-1\r\nM=-1\r\n(END_GT)");

            List<Token> currentTokens = tokenizer.ScanTokens();

            Assert.Equal(19, currentTokens[43].LineNumber);
        }

        [Fact]
        public void Tokenizer_Integration_Test_9()
        {
            Tokenizer tokenizer = new("@256\r\nD=A\r\n@SP\r\nM=D\r\n@133\r\n0;JMP\r\n@R15\r\nM=D\r\n@SP\r\nAM=M-1\r\nD=M\r\nA=A-1\r\nD=M-D\r\nM=0\r\n@END_EQ\r\nD;JNE\r\n@SP\r\nA=M-1\r\nM=-1\r\n(END_EQ)\r\n@R15\r\nA=M\r\n0;JMP\r\n@R15\r\nM=D\r\n@SP\r\nAM=M-1\r\nD=M\r\nA=A-1\r\nD=M-D\r\nM=0\r\n@END_GT\r\nD;JLE\r\n@SP\r\nA=M-1\r\nM=-1\r\n(END_GT)");

            List<Token> currentTokens = tokenizer.ScanTokens();

            Assert.Equal(19, currentTokens[44].LineNumber);
        }


        [Fact]
        public void Tokenizer_Integration_Test_19()
        {
            Tokenizer tokenizer = new("@256\r\nD=A\r\n@SP\r\nM=D\r\n@133\r\n0;JMP\r\n@R15\r\nM=D\r\n@SP\r\nAM=M-1\r\nD=M\r\nA=A-1\r\nD=M-D\r\nM=0\r\n@END_EQ\r\nD;JNE\r\n@SP\r\nA=M-1\r\nM=-1\r\n(END_EQ)\r\n@R15\r\nA=M\r\n0;JMP\r\n@R15\r\nM=D\r\n@SP\r\nAM=M-1\r\nD=M\r\nA=A-1\r\nD=M-D\r\nM=0\r\n@END_GT\r\nD;JLE\r\n@SP\r\nA=M-1\r\nM=-1\r\n(END_GT)");

            List<Token> currentTokens = tokenizer.ScanTokens();

            Assert.Equal(1, currentTokens[1].LineNumber);
        }
        [Fact]
        public void Tokenizer_Integration_Test_11()
        {
            Tokenizer tokenizer = new("@256\r\nD=A\r\n@SP\r\nM=D\r\n@133\r\n0;JMP\r\n@R15\r\nM=D\r\n@SP\r\nAM=M-1\r\nD=M\r\nA=A-1\r\nD=M-D\r\nM=0\r\n@END_EQ\r\nD;JNE\r\n@SP\r\nA=M-1\r\nM=-1\r\n(END_EQ)\r\n@R15\r\nA=M\r\n0;JMP\r\n@R15\r\nM=D\r\n@SP\r\nAM=M-1\r\nD=M\r\nA=A-1\r\nD=M-D\r\nM=0\r\n@END_GT\r\nD;JLE\r\n@SP\r\nA=M-1\r\nM=-1\r\n(END_GT)");

            List<Token> currentTokens = tokenizer.ScanTokens();

            Assert.Equal(1, currentTokens[2].LineNumber);
        }

        [Fact]
        public void Tokenizer_Integration_Test_12()
        {
            Tokenizer tokenizer = new("@256\r\nD=A\r\n@SP\r\nM=D\r\n@133\r\n0;JMP\r\n@R15\r\nM=D\r\n@SP\r\nAM=M-1\r\nD=M\r\nA=A-1\r\nD=M-D\r\nM=0\r\n@END_EQ\r\nD;JNE\r\n@SP\r\nA=M-1\r\nM=-1\r\n(END_EQ)\r\n@R15\r\nA=M\r\n0;JMP\r\n@R15\r\nM=D\r\n@SP\r\nAM=M-1\r\nD=M\r\nA=A-1\r\nD=M-D\r\nM=0\r\n@END_GT\r\nD;JLE\r\n@SP\r\nA=M-1\r\nM=-1\r\n(END_GT)");

            List<Token> currentTokens = tokenizer.ScanTokens();

            Assert.Equal(1, currentTokens[3].LineNumber);
        }
    }
}
