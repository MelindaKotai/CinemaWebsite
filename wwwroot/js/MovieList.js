$(document).ready(function () {
    var url = new URL(document.location);
    var params = url.searchParams;
    var f = params.get("f");
    if (f != null)
        $("#filter").val(f);
    else
        $("#filter").val("toate");
});


$(".isactive").change(function () {
    var id = $(this).val();
    $.post("/Movie/ChangeStatus/" + id, function (data) {
        $(".result").addClass("alert");
        $(".result").addClass("alert-info");
        $(".result").html(data);
    });

});


$("#filter").change(function () {
    $("#filterform").submit();

});