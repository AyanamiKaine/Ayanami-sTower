local DateTime = {}

function DateTime.nowToIso8601(timestamp)
    local isoString = os.date("!%Y-%m-%dT%H:%M:%SZ", timestamp)
    return isoString
end

function DateTime.now()
    return os.time()        -- Get current time in seconds since epoch
end

function DateTime.AddDays(time, daysToAdd)
    return time + daysToAdd * 24 * 60 * 60
end

function DateTime.AddMinutes(time, minutesToAdd)
   return time + minutesToAdd * 60
end


function DateTime.AddSeconds(time, secondsToAdd)
   return time + secondsToAdd
end

return DateTime
