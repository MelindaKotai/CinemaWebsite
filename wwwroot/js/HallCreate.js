
$("#generate").click(function () {
    //verifica daca s-au ales un numar de randuri si coloane

    var rows = $("#rows").val();
    var cols = $("#cols").val();
    if (rows == 0 || cols == 0) {
        $(".error").addClass("alert");
        $(".error").addClass("alert-danger");
        $(".error").html("Alegeți un număr de rânduri și de coloane!");

    } else {
        $(".error").removeClass("alert");
        $(".error").removeClass("alert-danger");
        $(".error").html("");
        var p = $("#paths").val().split(",");
        var width = 100 / cols;


        var html = "Selectați culoarele din sală: ";
        for (var i = 0; i < rows; i++) {
            html = html + "<div class='row mt-2 mb-2'>"
            for (var j = 0; j < cols; j++) {

                var elem = j + i * cols + 1;

                if (p.includes(elem.toString())) {
                    html = html + "<div id='" + elem + "' class='selected elem' style='width:" + width + "%;'></div>";

                } else {
                    html = html + "<div id='" + elem + "' class='seat free elem' style='width:" + width + "%'></div>";
                }

            }
            html = html + "</div>";
        }
        html = html + "<div id='create' class='btn btn-danger'>Creați sala </div>";

        $("#matrix").html(html);

        $('.elem').click(function (e) {

            //daca e path se face seat si se scoate din lista
            //daca e seat se face path si se adauga in lista
            if ($(e.target).hasClass('selected')) {

                $(e.target).removeClass('selected');
                $(e.target).addClass('free');
                p = $.grep(p, function (value) {
                    return value != e.target.id;

                });


            } else if ($(e.target).hasClass('free')) {

                $(e.target).removeClass('free');
                $(e.target).addClass('selected');

                p.push(this.id);
            }


        });


        $("#create").click(function () {
  //se verifica daca lista de culoare e goala si daca este se trimite un alert de confirmare a actiunii
            if (paths.length == 0) {
                confirm("Nu ați selectat nici un culoar. Sunteți sigur ca doriți să creați sala cu această structură?");
            }
            else {

                $("#paths").val(p.toString());
                $("#createhallform").submit();
            }

          


        });
    }


});

