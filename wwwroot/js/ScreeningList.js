//se ia data din querystring si se pune ca value in input
const urlParams = new URLSearchParams(window.location.search);
const dateParam = urlParams.get('date');
document.getElementById('hiddendate').value = dateParam;
document.getElementById('datepicker').value = dateParam;

function changedate() {
    var date = document.getElementById('datepicker').value;
    var queryParams = new URLSearchParams(window.location.search);
    queryParams.set("date", date);
    history.replaceState(null, null, "?" + queryParams.toString());
    location.reload();
}
function search() {
    var searchq = document.getElementById("searchinput").value;
    var queryParams = new URLSearchParams(window.location.search);
    queryParams.set("q", searchq);
    history.replaceState(null, null, "?" + queryParams.toString());
    location.reload();
}