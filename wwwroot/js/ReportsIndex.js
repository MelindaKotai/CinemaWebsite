
    //se initializeaza variabila currtable
    var currTable = $('#allmoviesttable');

   //se initializeaza variabila chart
    var myChart3 = new Chart(
    document.getElementById('myChart3')
    
    );

    //scripts care sa trimita ajax si sa aseze date
    //selectul pentru topuri
        $("#tops").on('change', function () {
            //se distruge chartul xistent in vanvas pt a se crea altul
            myChart3.destroy();
            //daca nu s-a selectat nici o optiune nu se intampla nimic
            var option = $("#tops").val();
            if (option == "") {
                return;
            }
            $.get("/Reports/GetTop?option=" + option, function (data) {

                $("#result").removeClass("hide");
                var content = "";
                //se verifica daca s-a reusit returnarea datelor
                if (data == "Error") {
                    content = "Generarea raportului a esuat!";
                    currTable.removeClass('hide');
                    currTable.addClass('hide');
                    $('#errorMessage').removeClass('hide');
                    currtable = $('#errormessage');
                    $('#errorMessage').html(content);
                    return;
                }
                //se contruieste obiectul js din stringul json returnat
                data = JSON.parse(data);
                currTable.removeClass('hide');
                currTable.addClass('hide');
                $("#" + option + "table").removeClass('hide');
                currTable = $("#" + option + "table");
                var contentid = "#" + option + "content";

                var datalabels = new Array();
                var datadata = new Array();
                for (var i = 0; i < data.length;i++) {
                    j = i + 1;
                    key = data[i].Key;
                    value = data[i].Value;
                    content = content + "<tr><td>" + j+ "</td><td>" + key + "</td><td>" + value + "</td></tr>";
                    datalabels[i] = data[i].Key;
                    datadata[i] = data[i].Value;
                }
                $(contentid).html(content);

                $('#downloadlink').html("<a class='nolink' href='/Reports/ExportTop?option="+option+"'>Descărcași datele în format csv</a>");

                const labels3 = datalabels;
                const data3 = {
                    labels: labels3,
                    datasets: [{
      
                        data: datadata,
                        backgroundColor: [
                            'rgb(209, 19, 19)',
                            'rgba(255, 99, 132)',
                            'rgba(255, 159, 64)',
                            'rgba(255, 205, 86)',
                            'rgba(75, 192, 192)',
                            'rgba(54, 162, 235)',
                            'rgba(153, 102, 255)',
                            'rgba(201, 203, 207)',
                            'rgb(255, 99, 132)',
                            'rgb(120, 53, 234)',
                            'rgb(234, 53, 87)',
                            'rgb(66, 53, 234)',
                            'rgb(54, 162, 235)',
                            'rgb(234, 53, 129)',
                            'rgb(192, 53, 234)',
                            'rgb(234, 195, 53)',
                            'rgb(234, 53, 228)',
                            'rgb(53, 234, 117)',
                            'rgb(183, 234, 53)',
                            'rgb(53, 234, 183)',
                            'rgb(234, 117, 53)'
                        ],
                        borderColor: [
                            'rgb(209, 19, 19)',
                            'rgba(255, 99, 132)',
                            'rgba(255, 159, 64)',
                            'rgba(255, 205, 86)',
                            'rgba(75, 192, 192)',
                            'rgba(54, 162, 235)',
                            'rgba(153, 102, 255)',
                            'rgba(201, 203, 207)',
                            'rgb(255, 99, 132)',
                            'rgb(120, 53, 234)',
                            'rgb(234, 53, 87)',
                            'rgb(66, 53, 234)',
                            'rgb(54, 162, 235)',
                            'rgb(234, 53, 129)',
                            'rgb(192, 53, 234)',
                            'rgb(234, 195, 53)',
                            'rgb(234, 53, 228)',
                            'rgb(53, 234, 117)',
                            'rgb(183, 234, 53)',
                            'rgb(53, 234, 183)',
                            'rgb(234, 117, 53)'
                        ],
                        borderWidth: 1,
                        color: 'white',
                    }]
                };

                const config3 = {
                    type: 'bar',
                    data: data3,
                    options: {
                    scales: {
                        y: {
                        beginAtZero: true
                           }
                        }
                    },
                };
                myChart3 = new Chart(
                    document.getElementById('myChart3'),
                    config3
                );

                myChart3.options.plugins.legend.display = false;

                myChart3.update();


            });
        });


        function destroyChart() {
        myChart3.destroy();
        }


        //la modificarea selectului rapoarte
        $("#reports").on('change', function () {
                myChart3.destroy();
                var option = $("#reports").val();
                if (option == "") {
                    return;
                }
                $.get("/Reports/GetReport?option=" + option, function (data) {

                    $("#result").removeClass("hide");
                    var content = "";
                    if (data == "Error") {
                        content = "Generarea raportului a esuat!";
                        currTable.removeClass('hide');
                        currTable.addClass('hide');
                        $('#errorMessage').removeClass('hide');
                        currtable = $('#errormessage');
                        $('#errorMessage').html(content);
                        return;
                    }

                    data = JSON.parse(data);
                    currTable.removeClass('hide');
                    currTable.addClass('hide');
                    $("#"+option+"table").removeClass('hide');
                    currTable = $("#"+option+"table");

                    var contentid = "#" + option + "content";


                    var datalabels = new Array();
                    var datadata = new Array();
                    for (var i = 0; i < data.length; i++) {
                        j = i + 1;
                        key = data[i].Key;
                        value = data[i].Value;
                        content = content + "<tr><td>" + key + "</td><td>" + value + "</td></tr>";
                        datalabels[i] = data[i].Key;
                        datadata[i] = data[i].Value;
                    }
                    $(contentid).html(content);

                    $('#downloadlink').html("<a class='nolink' href='/Reports/ExportReport?option=" + option + "'>Descarcati datele in format csv</a>");


                    const labels3 = datalabels;

                    const data3 = {
                        labels: labels3,
                        datasets: [{
                           
                            data: datadata,
                            backgroundColor: [
                                'rgb(209, 19, 19)',
                                'rgba(255, 99, 132)',
                                
                            ],
                            borderColor: [
                                'rgb(209, 19, 19)',
                                'rgba(255, 99, 132)',
                            
                            ],
                            borderWidth: 1,
                            color: 'white',
                        }]
                    };

                    const config3 = {
                        type: 'line',
                        data: data3,
                      
                    };
                    myChart3 = new Chart(
                        document.getElementById('myChart3'),
                        config3
                    );

                    myChart3.options.plugins.legend.display = false;

                    myChart3.update();


                });
            });




        //la modificarea datei la care se doreste sa se afle det5alii legate de vanzari
        $("#date").on('change', function () {
            myChart3.destroy();
            var option = $("#dailyreports").val();
            date = $("#date").val();
            if (option == "" || date=="") {
                return;
            }
            $.get("/Reports/GetDailyReport?option=" + option+"&date="+date, function (data) {

                $("#result").removeClass("hide");
                var content = "";
                if (data == "Error") {
                    content = "Generarea raportului a esuat!";
                    currTable.removeClass('hide');
                    currTable.addClass('hide');
                    $('#errorMessage').removeClass('hide');
                    currtable = $('#errormessage');
                    $('#errorMessage').html(content);
                    return;
                }

                data = JSON.parse(data);
                currTable.removeClass('hide');
                currTable.addClass('hide');
                $("#" + option + "table").removeClass('hide');
                currTable = $("#" + option + "table");

                var contentid = "#" + option + "content";


                var datalabels = new Array();
                var datadata = new Array();
                for (var i = 0; i < data.length; i++) {
                    j = i + 1;
                    key = data[i].Key;
                    value = data[i].Value;
                    content = content + "<tr><td>" + key + "</td><td>" + value + "</td></tr>";
                    datalabels[i] = data[i].Key;
                    datadata[i] = data[i].Value;
                }
                $(contentid).html(content);
                $('#downloadlink').html("<a class='nolink' href='/Reports/ExportDailyReport?option=" + option + "&date="+date+"'>Descarcati datele in format csv</a>");



                const labels3 = datalabels;

                const data3 = {
                    labels: labels3,
                    datasets: [{
                      
                        data: datadata,
                        backgroundColor: [
                            'rgb(209, 19, 19)',
                            'rgba(255, 99, 132)',
                            'rgba(255, 159, 64)',
                            'rgba(255, 205, 86)',
                            'rgba(75, 192, 192)',
                            'rgba(54, 162, 235)',
                            'rgba(153, 102, 255)',
                            'rgba(201, 203, 207)',
                            'rgb(255, 99, 132)',
                            'rgb(120, 53, 234)',
                            'rgb(234, 53, 87)',
                            'rgb(66, 53, 234)',
                            'rgb(54, 162, 235)',
                            'rgb(234, 53, 129)',
                            'rgb(192, 53, 234)',
                            'rgb(234, 195, 53)',
                            'rgb(234, 53, 228)',
                            'rgb(53, 234, 117)',
                            'rgb(183, 234, 53)',
                            'rgb(53, 234, 183)',
                            'rgb(234, 117, 53)'

                        ],
                       
                        borderWidth: 1,
                        color: 'white',
                    }]
                };

                const config3 = {
                    type: 'pie',
                    data: data3,

                };
                myChart3 = new Chart(
                    document.getElementById('myChart3'),
                    config3
                );

                myChart3.options.plugins.legend.display = false;

                myChart3.update();
            });
        });

