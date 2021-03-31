
$("#generate").click(function () {
    //verifica daca s-au ales un numar de randuri si coloane

    var rows = $("#rows").val();
    var cols = $("#cols").val();
    if (rows == 0 || cols == 0) {
        $(".error").addClass("alert");
        $(".error").addClass("alert-danger");
        $(".error").html("Alegeti un numar de randuri si de coloane!");

    } else {
        $(".error").removeClass("alert");
        $(".error").removeClass("alert-danger");
        $(".error").html("");
        var p = $("#paths").val().split(",");
        var width = 100 / cols;

        //for i de la 1 la nr randuri//i nr randuri coloane se adauga la un string html//pt fiecare element se verifica daca se afla in p si daca da i se pune clasa path


        var html = "Selectati culoarele din sala: ";
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
        html = html + "<div id='create' class='btn btn-danger'>Creati sala </div>";

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

            if (paths.length == 0) {
                confirm("Nu ati selectat nici un culoar. Sunteti sigur ca doriti sa creati sala cu aceasta structura?");
            }
            else {

                $("#paths").val(p.toString());
                $("#createhallform").submit();
            }

            //se verifica daca lista de culoare e goala si daca este se trimite un alert de confirmare a actiunii


        });
    }


});

