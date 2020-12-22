"use strict";
var hasMissionNumberAssigned = false;
var globalMissioNumber = null;
function getUrlVars() {
    var vars = {};
    var parts = window.location.href.replace(/[?&]+([^=&]+)=([^&]*)/gi, function (m, key, value) {
        vars[key] = value;
    });
    return vars;
}

function getIdFromURL() {
    var vars = getUrlVars();
    return vars.featureId;
}

function setValue(divID, value) {
    document.getElementById(divID).innerHTML = value;
    $("#featureIdForm").val(value);
}

function closeWindow() {
    window.location.replace("https://tfsweb.tamu.edu");
}

function setFeatureId(divID) {
    var featureId = getIdFromURL();
    var featureIdError;
    if (typeof featureId === "undefined") {
        document.getElementById(divID).innerHTML = "<font class='warning'>Invalid/Expired URL. Please check your URL and try again.<br> Redirecting to TFS HomePage in 5 seconds.</font>";
        featureIdError = true;
        window.setTimeout(function() {
            window.location = "https://tfsweb.tamu.edu";
        }, 500000);
    } else {
        $.ajax({url: "./api/arcgis/"+ featureId, 
        async: false,
        success: function(result){
            if (result.featureDetails === "") {
                $("#fullForm").hide();
                $("#genericError").show(200);
                featureId = null;
            } else {
                ($("#featureId")).html(result.featureDetails);
                $("#featureIdForm").val(featureId);
                $("#email").val(result.email);
            }
          }});
    }
    return featureId;
}

function hasMissionNumber(featureId) {
    $.ajax({url: "./api/arcgis/hasMissionNumber/"+ featureId, 
    async: false,
    success: function(result){
        if (result === true){
            hasMissionNumberAssigned = true;
        } else { 
            hasMissionNumberAssigned = false;
            getNextMissionNumber(featureId);
        }
        toggleMissionNumberAssignedMessage(result);
      }});
}


function toggleMissionNumberAssignedMessage(result){
    if (result === true) {
        $("#decisionForm").hide();
        $("#missionNumberAlreadyAssigned").show(200);
    } else {
        $("#fullForm").show();
        $("#decisionForm").show(200);
        $("#missionNumberAlreadyAssigned").hide();
    }
}

function getNextMissionNumber(featureId) {
    $.ajax({url: "./api/arcgis/getMissionNumber/"+ featureId, 
    async: true,
    success: function(result){
        if (result.missionNumber === ""){
           $("#missionNumberInput").val("");
        } else {
            globalMissioNumber = result.missionNumber;
            $("#missionNumberInput").val(result.missionNumber);
        }
    }});
}

function radioChange() {
    var decision = $("input[name='decision']:checked").val();
    if(decision === "approved") {
        $("#missionNumberDiv").show(500);
        $("#missionNumberInput").val(globalMissioNumber);
    } else {
        $("#missionNumberDiv").hide(500);
        $("#missionNumberInput").val('');
    }
}

function submitDecision() {
    var decision = $("input[name='decision']:checked").val();
    var hasDecisionError = (typeof decision === "undefined");
    if (hasDecisionError) {
        $("#decisionError").show(300);
    } else {$("#decisionError").hide(300);}
    var committeeMemberName = $("#committeeMemberName").children("option:selected").val();
    var hasMemberNameError =typeof committeeMemberName === "undefined" || committeeMemberName === "";
    if (hasMemberNameError) {
        $("#memberNameError").show(300);
    } else { $("#memberNameError").hide(300);}
    var missionNumber =  $("input[name='missionNumber']").val();
    var hasMissionError = (isNaN(parseInt(missionNumber)) || missionNumber.length < 10) && (decision !== "denied");
    var featureId =  $("#featureIdForm").val();
    var comments =  $("#comments").val();
    var email = $("#email").val();
    if (hasMissionError) {
        $("#missionNumberError").show(300);
    } else {$("#missionNumberError").hide(300);}
    if (hasDecisionError || hasMissionError || hasMemberNameError) {
        
    } else {
        if (decision === "denied"){
            missionNumber = ""
            //setValue("featureId", "")
        }
        submitForm(decision, committeeMemberName, missionNumber, featureId, comments, email);
    }
}

function submitForm(decision, committeeMemberName, missionNumber, featureId, comments, email) {
    // check for the mission number again
  
        $.ajax({
            url: "./api/arcgis/hasMissionNumber/" + featureId,
            async: false,
            success: function (result) {
                if (result === true) {
                    alert("This request already has a mission number assigned. Please email uastech@tfs.tamu.edu to remove it and reassign one ");
                    window.location = "https://tfsweb.tamu.edu";
                } else {
                    $.ajax({
                        method: "POST",
                        url: "./api/arcgis/",
                        contentType: "application/json",
                        dataType: 'json',
                        async: true,
                        data: JSON.stringify({
                            "featureId": featureId,
                            "missionNumber": missionNumber,
                            "committeeDecision": decision,
                            "committeeMemberName": committeeMemberName,
                            "committeeRemarks": comments,
                            "email": email
                        }),
                        success: function (msg) {
                            decisionResponse(msg);
                        }
                    })
                }
               
            }
        });



}

function decisionResponse(msg) {
    if (msg.success === true) {
        $("#fullForm").hide();
        $("#sendersEmail").html($("#email").val());
        $("#successMessage").show(200);
        $("#errorMessage").hide();
    } else {
        $("#errorMessage").show(200);
        $("#successMessage").hide();
    }
}

function onBodyLoad() {

    $(document).ajaxStart(function() {
        $("#loadingImageDiv").show();
      });
      
      $(document).ajaxStop(function() {
        $("#loadingImageDiv").hide();
      });


    $("#missionNumberDiv").hide();
    var featureId = setFeatureId("featureId");
    if (featureId !== null) {
        hasMissionNumber(featureId);
    }
    
    $("input[name='decision']").on("change", radioChange);

    $("#decisionForm").submit(function(e){
        e.preventDefault();
        submitDecision();
    });
}


