$(".status").change(function () {
    var id = $(this).val();
    $.post("/TicketTypes/ChangeStatus/" + id, function (data) {
        $(".result").addClass("alert");
        $(".result").addClass("alert-info");
        $(".result").html(data);
    });

});
