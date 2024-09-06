local json = require "./Json/json"
local StellaSockets = require "Sockets.StellaSockets"
local StellaTesting = require "StellaTesting.StellaTesting"
local StellaDatabase = require "./Database/StellaDatabase"
local StellaDataTime = require "DateTime.DateTime"
local StellaSTL = {
    json = json,
    StellaSockets = StellaSockets,
    StellaTesting = StellaTesting,
    StellaDatabase = StellaDatabase,
    StellaDataTime = StellaDataTime
}
return StellaSTL