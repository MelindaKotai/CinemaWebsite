$(".status").change(function () {
    var id = $(this).val();
    $.post("/Prize/ChangeStatus/" + id, function (data) {
        $(".result").addClass("alert");
        $(".result").addClass("alert-info");
        $(".result").html(data);
    });

});