﻿
   

    let theWheel = new Winwheel({

        'numSegments': segments,
        'lineWidth': 3,
        'textFontFamily': 'Verdana',
        'textFontSize': 13,

       'responsive':true,


        'animation':
        {
        'type': 'spinToStop',
            'duration': 3,
            'spins': 5,
            'callbackFinished': alertPrize
        }
    });


        let wheelPower = 0;



        let wheelSpinning = false;

      function  startSpin() {

            if (document.getElementById('spinbutton').disabled != true) {

                if (wheelPower == 1) {
        theWheel.animation.spins = 2;
                } else if (wheelPower == 2) {
        theWheel.animation.spins = 5;
                } else if (wheelPower == 3) {
        theWheel.animation.spins = 10;
                }
                wheelSpinning == true;
            document.getElementById('spinbutton').disabled = true;
            theWheel.startAnimation();


        }
        }





        function powerSelected(powerLevel) {

            if (wheelSpinning == false) {

        document.getElementById('pw1').className = "";
                document.getElementById('pw2').className = "";
                document.getElementById('pw3').className = "";


                if (powerLevel >= 1) {
        document.getElementById('pw1').className = "pw1";
                }

                if (powerLevel >= 2) {
        document.getElementById('pw2').className = "pw2";
                }

                if (powerLevel >= 3) {
        document.getElementById('pw3').className = "pw3";
                }


                wheelPower = powerLevel;


                document.getElementById("spinbutton").disabled = false;


            }
        }




            window.onresize=function() {

                var height = document.getElementById('canvas').offsetHeight;
                height = height + 146;
                var width = document.getElementById('canvas').offsetWidth;


                document.getElementById('the_wheel').setAttribute("style", "height:" + height + "px;width: " + width + "px");
            };
      