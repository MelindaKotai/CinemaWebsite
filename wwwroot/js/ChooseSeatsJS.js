$('.seat').click(function (event) {

    //daca este un scaun deja rezervat nu se permite selectarea lui
    if ($(event.target).hasClass('reserved')) {
        alert('Acest loc este deja rezervat si nu poate fi selectat');
    }
    //daca este un scaun liber se permite selectarea se adauga in array

    if ($(event.target).hasClass('free')) {
        if (noseats > 0) {
            $(event.target).removeClass('free');
            $(event.target).addClass('selected');
            noseats = noseats - 1;
            seats.push(event.target.id);
        } else alert("Ati selectat toate locurile");
    }

    else if ($(event.target).hasClass('selected')) {
        $(event.target).removeClass('selected');
        $(event.target).addClass('free');
        noseats = noseats + 1;
        seats = $.grep(seats, function (value) {
            return value != event.target.id;
        });

    }
});

$('.reserve').click(function (e) {
    if (noseats == 0) {
        $('#selectedseats').val(seats.toString());
        $('#reserveform').submit();
    } else {
        e.preventDefault();
        alert("Mai aveti " + noseats + " locuri de selectat!");
    }
});