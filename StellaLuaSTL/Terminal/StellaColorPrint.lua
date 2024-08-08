local ColorPrint = {
    CRED = '\x1b[91m',
    CGREEN = '\x1b[92m',
    CYELLOW = '\x1b[93m',
    CBlue = '\x1b[94m',
    CEND = '\x1b[0m',
}

function ColorPrint.Green(text)
    print(ColorPrint.CGREEN .. text .. ColorPrint.CEND)
end

function ColorPrint.Red(text)
    print(ColorPrint.CRED .. text .. ColorPrint.CEND)
end

function ColorPrint.Yellow(text)
    print(ColorPrint.CYELLOW .. text .. ColorPrint.CEND)
end

function ColorPrint.Blue(text)
    print(ColorPrint.CBlue .. text .. ColorPrint.CEND)
end

return ColorPrint
