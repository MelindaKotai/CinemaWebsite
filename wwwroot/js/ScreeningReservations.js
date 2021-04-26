$(".state").change(function () {
    var id = $(this).val();
    $.post("/Reservation/ChangeStatus/" + id, function (data) {
        $(".result").addClass("alert");
        $(".result").addClass("alert-info");
        $(".result").html(data);
    });

});

$('.deletebtn').on('click', function () {
    return confirm('Sunteti sigur ca doriti sa stergeti aceasta rezervare ?');
});

