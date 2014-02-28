function GetCrawlerState() {
    $.ajax({
        type: "POST",
        url: "admin.asmx/GetCmd",
        data: JSON.stringify({}),
        contentType: "application/json; charset=utf-8",
        dataType: 'json',
        success: function (data) {
            (JSON.parse(data.d) == "stop") ? $("#crawler-state").html("idle") : $("#crawler-state").html("crawling");
        }
    });
}

function StartCrawler() {
    $.ajax({
        type: "POST",
        url: "admin.asmx/StartCrawler",
        data: JSON.stringify({}),
        contentType: "application/json; charset=utf-8",
        dataType: 'json',
        success: function (data) {
        }
    });
}

function StopCrawler() {
    $.ajax({
        type: "POST",
        url: "admin.asmx/StopCrawler",
        data: JSON.stringify({}),
        contentType: "application/json; charset=utf-8",
        dataType: 'json',
        success: function (data) {
        }
    });
}

function GetSizeOfUrlQueue() {
    $.ajax({
        type: "POST",
        url: "admin.asmx/GetSizeOfUrlQueue",
        data: JSON.stringify({}),
        contentType: "application/json; charset=utf-8",
        dataType: 'json',
        success: function (data) {
            $("#size-of-queue").html(JSON.parse(data.d));
        }
    });
}

function Ram() {
    $.ajax({
        type: "POST",
        url: "admin.asmx/Ram",
        data: JSON.stringify({}),
        contentType: "application/json; charset=utf-8",
        dataType: 'json',
        success: function (data) {
            $("#ram-available").html(JSON.parse(data.d));
        }
    });
}

function Cpu() {
    $.ajax({
        type: "POST",
        url: "admin.asmx/Cpu",
        data: JSON.stringify({}),
        contentType: "application/json; charset=utf-8",
        dataType: 'json',
        success: function (data) {
            $("#cpu-utilization").html(JSON.parse(data.d));
        }
    });
}

function ClearAll() {
    $.ajax({
        type: "POST",
        url: "admin.asmx/ClearAll",
        data: JSON.stringify({}),
        contentType: "application/json; charset=utf-8",
        dataType: 'json',
        success: function (data) {
            if (JSON.parse(data.d)) {
                $("#feedback").html("Index cleared, queue cleared, crawler stopped");
            }
        }
    });
}


function GetPageTitle() {
    $.ajax({
        type: "POST",
        url: "admin.asmx/GetPageTitle",
        data: JSON.stringify({ url: $("#s").val().trim()}),
        contentType: "application/json; charset=utf-8",
        dataType: 'json',
        success: function (data) {
            $("#resultSet").html((JSON.parse(data.d)));
        }
    });
}

function GetStats() {
    GetCrawlerState();
    Ram();
    Cpu();
    GetSizeOfUrlQueue();
}

function loop() {
    GetStats();
    setTimeout("loop()", 3000);
}
setTimeout(GetStats(), 1000);