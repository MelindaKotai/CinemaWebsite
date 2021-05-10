$(document).ready(function () {
    var stripe = Stripe('pk_test_51ImDqrLWDtGNpb5wYXykPzGz15FQ4M2iwfn5GrNeau9cuKJgbtlU1S78OAeGmrYaleSc0NGDkfW5PiHdeCM3673c00MH5Ucd4N');

    var elements = stripe.elements();


    var style = {
        base: {

            'lineHeight': '1.35',
            'fontSize': '1.11rem',
            'color': '#495057',
            'fontFamily': 'apple-system,BlinkMacSystemFont,"Segoe UI",Roboto,"Helvetica Neue",Arial,sans-serif'
        }
    };

    //SE CREAZA INPUTURILE PT CARDNUMBER,CVC SI EXP
    var card = elements.create('cardNumber', { 'placeholder': '1234 1234 1234 1234', 'style': style });
    card.mount("#card-number");

    var cvc = elements.create('cardCvc', { 'placeholder': 'yyy', 'style': style });
    cvc.mount("#card-cvc");

    var exp = elements.create('cardExpiry', { 'placeholder': 'xx/xx', 'style': style });
    exp.mount("#card-exp");

    var form = document.getElementById('payment-form');
    form.addEventListener('submit', function (event) {
        event.preventDefault();
        stripe.createToken(card).then(function (result) {
            if (result.error) {
                var errorElement = element.getElementById("card-errors");
                errorElement.textContent = result.error.message;
            } else {
                stripeTokenHandler(result.token);
                console.log(result.token);
            }
        });
    });

    function stripeTokenHandler(token) {
        $(".StripeToken").val(token.id);
        form.submit();
    }
});
