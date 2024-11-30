/* UTILITY FUNCTIONS */

function httpGet(path, successCallback, failCallback) {
    var xmlHttp = new XMLHttpRequest();
    xmlHttp.onreadystatechange = () => {
        if (xmlHttp.readyState == 4 && xmlHttp.status == 200) {
            successCallback(JSON.parse(xmlHttp.responseText));
        } else if (xmlHttp.readyState == 4) {
            failCallback();
        }
    }
    xmlHttp.open("GET", path, true);
    xmlHttp.send(null);
}

function httpPost(path, body, method, successCallback, failCallback) {
    var xmlHttp = new XMLHttpRequest();
    xmlHttp.onreadystatechange = () => {
        if (xmlHttp.readyState == 4 && xmlHttp.status == 200) {
            successCallback(JSON.parse(xmlHttp.responseText));
        } else if (xmlHttp.readyState == 4) {
            failCallback();
        }
    }
    xmlHttp.open(method, path, true);
    xmlHttp.send(JSON.stringify(body));
}

function createDom(type, className, parent, text) {
    var d = document.createElement(type);
    if (className != null) {
        d.classList.add(className);
    }
    if (parent != null) {
        parent.appendChild(d);
    }
    if (text != null) {
        d.innerText = text;
    }
    return d;
}

var DATE_MONTH_NAMES = [
    "January",
    "February",
    "March",
    "April",
    "May",
    "June",
    "July",
    "August",
    "September",
    "October",
    "November",
    "December"
]

function createTimeString(hours, mins) {
    //Create hours
    var hourStr;
    var amPm;
    if (hours == 0) {
        hourStr = "12";
        amPm = "AM";
    } else if (hours < 13) {
        hourStr = hours.toString();
        amPm = "AM";
    } else {
        hourStr = (hours - 12).toString();
        amPm = "PM";
    }

    return hourStr + ":" + mins.toString().padStart(2, '0') + " " + amPm;
}

// Creates a form item and returns the body DOM
function createFormItem(container, title, subtext) {
    var root = createDom("div", "form_item", container);
    createDom("div", "form_heading", root, title);
    return createDom("div", "form_content", root);
}

/* MODAL */

var currentModal = null;

//Data is a key value pair of
//title   : The title to display
//content : Content DOM object to insert
//buttons : An array of buttons to show, with each item following this schema:
//          title     : Text to display
//          classname : Classname to put on the button, optional.
//          action    : Callback to fire when button is clicked. First argument is button DOM.
function showModal(data) {
    //Create
    var container = createDom("div", "modal_container");
    container.classList.add("modal_container_closing");
    var winContainer = createDom("div", "modal_window_container", container);
    var win = createDom("div", "modal_window", winContainer);
    var header = createDom("div", "modal_window_header", win, data.title);
    var closeBtn = createDom("div", "modal_window_header_btn", header);
    var contentContainer = createDom("div", "modal_window_content", win);
    contentContainer.appendChild(data.content);
    var footer = createDom("div", "modal_window_footer", win);
    var footerNav = createDom("div", "modal_window_footer_nav", footer);

    //Create buttons
    for (var i = 0; i < data.buttons.length; i++) {
        var btnData = data.buttons[i];
        var btn = createDom("div", "modal_window_footer_nav_btn", footerNav, btnData.title);
        btn.__btn_data = btnData;
        if (btnData.classname != null) {
            btn.classList.add(btnData.classname);
        }
        btn.addEventListener("click", function () {
            //Fire event
            this.__btn_data.action(this);
        });
    }

    //Store
    currentModal = container;

    //Add events
    closeBtn.addEventListener("click", () => {
        closeModal();
    });

    //Attach
    document.body.appendChild(container);

    //Wait a moment for animation
    window.requestAnimationFrame(() => {
        container.classList.remove("modal_container_closing");
    });
}

function closeModal() {
    var m = currentModal;
    if (m != null) {
        //Clear to prevent multiple
        currentModal = null;

        //Add class to start animation
        m.classList.add("modal_container_closing");

        //Clean up
        window.setTimeout(() => {
            m.remove();
        }, 150);
    }
}

/* CORE */

var netInfo; // Info data from the API
var siteStatus = {}; // Status DOMs for each site
var expertMode = false;

// Helper function for createCommandForm
function createCommandFormSelectSiteOption(selectContainer, siteIndex) {
    for (var i = 0; i < netInfo.sites[siteIndex].sources.length; i++) {
        //Get info
        var srcInfo = netInfo.sites[siteIndex].sources[i];

        //Only show normal sources unless advanced mode is on
        if (srcInfo.type == "NORMAL" || expertMode) {
            var o = createDom("option", null, selectContainer, srcInfo.name);
            o.value = siteIndex + ":" + srcInfo.index;
        }
    }
}

// Creates a form for setting up a simulcast. If thisSiteIndex is >= 0, only sources belonging to it will be shown. Otherwise all are shown.
// Returns an opaque type that can be passed into readCommandForm.
// If command != null, this data is used to set defaults.
function createCommandForm(thisSiteIndex, container, command) {
    //Create the selection dialog
    var srcItem = createFormItem(container, "Send Source...", "");
    var srcSelect = createDom("select", "form_select", srcItem);
    if (thisSiteIndex == -1) {
        //Show all sites sources
        for (var i = 0; i < netInfo.sites.length; i++) {
            //Create group
            var o = createDom("optgroup", null, srcSelect);
            o.label = netInfo.sites[i].name;

            //Create this site
            createCommandFormSelectSiteOption(o, i);
        }
    } else {
        //Show only sources belonging to this site
        createCommandFormSelectSiteOption(srcSelect, thisSiteIndex);
    }

    //Create site checkbox list
    var siteItem = createFormItem(container, "To sites...", "");
    var siteCheckContainer = createDom("div", null, siteItem);
    var siteChecks = [];
    for (var i = 0; i < netInfo.sites.length; i++) {
        //Create container
        var checkContainer = createDom("div", null, siteCheckContainer);

        //Create check
        var c = createDom("input", "form_check", checkContainer);
        c.type = "checkbox";
        c.id = "fcheck_" + i;

        //Create label
        var l = createDom("label", null, checkContainer, netInfo.sites[i].name);
        l.htmlFor = "fcheck_" + i;

        //If a site index was specified, automatically check it
        if (netInfo.sites[i].index == thisSiteIndex) {
            c.checked = true;
            c.disabled = true;
        }

        //Add to dict
        c.__site_index = netInfo.sites[i].index;
        siteChecks.push(c);
    }

    //If command != null, set it
    if (command != null) {
        //Set from station
        srcSelect.value = command.from_site.toString() + ":" + command.from_site_source.toString();

        //Set checks
        for (var i = 0; i < siteChecks.length; i++) {
            if (command.to_sites.includes(siteChecks[i].__site_index)) {
                siteChecks[i].checked = true;
            }
        }
    }

    //If all sites are listed, add a change event to the dropdown that'll automatically grey out the checkbox for the site it's from
    if (thisSiteIndex == -1) {
        var evt = () => {
            //Get the index of the currently selected site
            var selectedSiteIndex = parseInt(srcSelect.value.split(':')[0]);

            //Grey out this check and nothing else
            for (var i = 0; i < siteChecks.length; i++) {
                var beDisabled = siteChecks[i].__site_index == selectedSiteIndex;
                if (beDisabled && !siteChecks[i].disabled) {
                    //Make it disabled but save previous state
                    siteChecks[i].__old_state = siteChecks[i].checked;
                    siteChecks[i].checked = true;
                    siteChecks[i].disabled = true;
                } else if (!beDisabled && siteChecks[i].disabled) {
                    //Should no longer be disabled; Restore state
                    siteChecks[i].disabled = false;
                    if (siteChecks[i].__old_state != null) {
                        siteChecks[i].checked = siteChecks[i].__old_state;
                    }
                }
            }
        }
        srcSelect.addEventListener("change", evt);
        evt();
    }

    //Build result
    return {
        "srcSelect": srcSelect,
        "siteChecks": siteChecks
    };
}

// Reads a command form created by createCommandForm and returns a command that can be sent. Returns null if it's invalid.
function readCommandForm(formData) {
    //Read the selected source
    if (formData.srcSelect.value == null) { return null; }
    var selectedSrcComponents = formData.srcSelect.value.split(':');
    if (selectedSrcComponents.length != 2) { return null; }
    var selectedSite = parseInt(selectedSrcComponents[0]);
    var selectedSrc = parseInt(selectedSrcComponents[1]);

    //Read the to site checks
    var toSiteIndicies = [];
    for (var i = 0; i < formData.siteChecks.length; i++) {
        //Get info
        var siteCheck = formData.siteChecks[i];
        var siteIndex = siteCheck.__site_index;

        //Add if checked and it does not match this site
        if (siteCheck.checked && siteIndex != selectedSite) {
            toSiteIndicies.push(siteIndex);
        }
    }

    //Create command
    return {
        "from_site": selectedSite,
        "from_site_source": selectedSrc,
        "to_sites": toSiteIndicies
    };
}

// Function that makes all sites in the status section show loading.
function clearStatus() {
    for (var i = 0; i < netInfo.sites.length; i++) {
        var statusDom = siteStatus[netInfo.sites[i].index];
        statusDom.root.classList.add("main_status_site_state_loading");
        statusDom.root.classList.remove("main_status_site_state_error");
        statusDom.root.classList.remove("main_status_site_state_tx");
        statusDom.root.classList.remove("main_status_site_state_rx");
        statusDom.status.innerText = "Loading...";
    }
}

// Function that refreshes status section after it has been created
function refreshStatus() {
    //Clear
    clearStatus();

    //Request
    httpGet("/api/status", (status) => {
        //We're going to swap it to figure out what sites are currently reciving. Init this array, which is a key (index of site) dict with values of arrays with sites that are sending to it
        var rxState = {};
        for (var i = 0; i < status.sites.length; i++) {
            rxState[status.sites[i].index] = [];
        }

        //Loop through each site and build a database of what sites are reciving
        for (var i = 0; i < status.sites.length; i++) {
            for (var j = 0; j < status.sites[i].codec.transmitting_to.length; j++) {
                rxState[status.sites[i].codec.transmitting_to[j]].push(status.sites[i].index);
            }
        }

        //Loop through each site
        for (var i = 0; i < status.sites.length; i++) {
            //Get parts
            var siteStat = status.sites[i];
            var siteInfo = netInfo.sites[siteStat.index];
            var statusDom = siteStatus[status.sites[i].index];

            //Clear
            statusDom.root.classList.remove("main_status_site_state_loading");

            //Determine the name of the site being transmitted from the switcher. If no switcher is present, get the first site from the info
            var txSrc = 0;
            if (siteStat.switcher != null) {
                txSrc = siteStat.switcher.selected_source;
            }

            //Get name of the site (or default if invalid)
            var txSrcName = "Unknown";
            if (txSrc >= 0) {
                txSrcName = siteInfo.sources[txSrc].name;
            }

            //Switch based on status
            if (!siteStat.codec.success) { // Errored - Codec unreachable
                //Set to errored
                statusDom.root.classList.add("main_status_site_state_error");
                statusDom.status.innerText = "Error (codec unreachable)";
            } else if (siteStat.switcher != null && !siteStat.switcher.success) { // Errored - Switcher unreachable
                //Set to errored
                statusDom.root.classList.add("main_status_site_state_error");
                statusDom.status.innerText = "Error (switcher unreachable)";
            } else if (siteStat.codec.transmitting_to.length > 0) { // Transmitting
                //Set to TX status
                statusDom.root.classList.add("main_status_site_state_tx");

                //Set
                statusDom.status.innerText = "Sending " + txSrcName;
            } else if (rxState[siteStat.index].length > 0) { // Reciving
                //Set to RX status
                statusDom.root.classList.add("main_status_site_state_rx");

                //If there is one, get the name from that site in the info. Otherwise, show multiple.
                if (rxState[siteStat.index].length == 1) {
                    //Figure out the name of the source the rx site is sending
                    var rxSite = netInfo.sites[rxState[siteStat.index][0]];
                    var rxSiteStatus = status.sites[rxSite.index];
                    var rxSrcName;
                    if (rxSiteStatus.switcher == null) {
                        // No switcher; Use the first source
                        rxSrcName = rxSite.sources[0].name;
                    } else if (!rxSiteStatus.switcher.success || rxSiteStatus.switcher.selected_source < 0) {
                        // Switcher is unavailable or has an unknown selection.
                        rxSrcName = "???";
                    } else {
                        // Switcher has valid data; Get the name of the source
                        rxSrcName = rxSite.sources[rxSiteStatus.switcher.selected_source].name;
                    }

                    //Set
                    statusDom.status.innerText = "Receiving " + rxSrcName + " from " + rxSite.name;
                } else {
                    statusDom.status.innerText = "Receiving from MULTIPLE";
                }
            } else if (siteStat.switcher != null && txSrc >= 0 && siteInfo.sources[txSrc].type != "EXTERNAL") { // Idle, but could be sending something to local site
                //Set
                statusDom.status.innerText = "Idle (" + txSrcName + " active)";
            } else { // Idle
                //Idle
                statusDom.status.innerText = "Idle";
            }
        }
    }, () => {
        alert("Failed to get status info. Try refreshing.");
    });
}

// Called when a site in status is clicked to be changed
function showSiteSetup(srcSiteIndex) {
    //Build form
    var content = createDom("div", "form_body");
    var cmdForm = createCommandForm(srcSiteIndex, content);

    //Show
    var isLoading = false;
    showModal({
        "title": "Simulcast From " + netInfo.sites[srcSiteIndex].name + "...",
        "content": content,
        "buttons": [
            {
                "title": "Apply",
                "classname": "modal_window_footer_nav_btn_blu",
                "action": (btnDom) => {
                    if (!isLoading) {
                        //Read the command
                        var cmd = readCommandForm(cmdForm);

                        //If invalid, abort
                        if (cmd == null) {
                            alert("Invalid setup!");
                            return;
                        }

                        //Set loading flag so we don't try and do it multiple times
                        isLoading = true;

                        //Set button to be spinner
                        btnDom.classList.add("modal_window_footer_nav_btn_loading");

                        //Send
                        var req = {
                            "command": cmd
                        }
                        httpPost("/api/modify", req, "POST", (result) => {
                            if (result.success) {
                                //Close modal
                                closeModal();

                                //Clear status
                                clearStatus();

                                //Refresh status after the specified recommended delay
                                window.setTimeout(() => {
                                    refreshStatus();
                                }, result.delay);
                            } else {
                                //Server error - Clear to try again
                                alert("Error: " + result.message);
                                btnDom.classList.remove("modal_window_footer_nav_btn_loading");
                                isLoading = false;
                            }
                        }, () => {
                            //Unknown server error - Clear to try again
                            alert("Server error.");
                            btnDom.classList.remove("modal_window_footer_nav_btn_loading");
                            isLoading = false;
                        });
                    }
                }
            }
        ]
    });
}

// Shows the modal for editing or creating an event. If eventData is null, defaults will be used
function showEditEventDialog(eventData) {
    //Build form
    var content = createDom("div", "form_body");

    //Title
    var titleSec = createFormItem(content, "Title", "");
    var titleInput = createDom("input", "form_input", titleSec);
    if (eventData != null) {
        titleInput.value = eventData.description;
    }

    //Date
    var dateSec = createFormItem(content, "Scheduled Time", "");
    var dateInput = createDom("input", "form_input", dateSec);
    dateInput.type = "date";
    var timeInput = createDom("input", "form_input", dateSec);
    timeInput.type = "time";
    if (eventData != null) {
        var date = new Date(eventData.time);
        dateInput.value = date.getFullYear() + "-" + (date.getMonth() + 1).toString().padStart(2, '0') + "-" + date.getDate().toString().padStart(2, '0');
        timeInput.value = date.getHours() + ":" + date.getMinutes().toString().padStart(2, '0');
    }

    //Command info
    var cmdForm = createCommandForm(-1, content, eventData != null ? eventData.command : null);

    //Show
    var isLoading = false;
    showModal({
        "title": (eventData == null ? "Create" : "Update") + " Event",
        "content": content,
        "buttons": [
            {
                "title": eventData == null ? "Create" : "Update",
                "classname": "modal_window_footer_nav_btn_blu",
                "action": (btnDom) => {
                    if (!isLoading) {
                        //Read the parts
                        var datTitle = titleInput.value;
                        var datDate = dateInput.value;
                        var datTime = timeInput.value;
                        var cmd = readCommandForm(cmdForm);

                        //If invalid, abort
                        if (datTitle.length == 0) {
                            alert("Name of event is required.");
                            return;
                        }
                        if (datDate.length == 0 || datTime.length == 0) {
                            alert("Date/time of event is required.");
                            return;
                        }
                        if (cmd == null) {
                            alert("Invalid setup!");
                            return;
                        }

                        //Set loading flag so we don't try and do it multiple times
                        isLoading = true;

                        //Set button to be spinner
                        btnDom.classList.add("modal_window_footer_nav_btn_loading");

                        //Send
                        var req = {
                            "time": new Date(datDate + " " + datTime),
                            "description": datTitle,
                            "command": cmd
                        }
                        if (eventData != null) {
                            req["id"] = eventData.id;
                        }
                        httpPost("/api/events", req, eventData == null ? "PUT" : "PATCH", (result) => {
                            //Close modal
                            closeModal();

                            //Refresh events
                            refreshEvents();
                        }, () => {
                            //Unknown server error - Clear to try again
                            alert("Server error.");
                            btnDom.classList.remove("modal_window_footer_nav_btn_loading");
                            isLoading = false;
                        });
                    }
                }
            }
        ]
    });
}

// Shows a confirmation dialog to delete an event
function showDeleteEventDialog(eventData) {
    //Build dialog
    var content = createDom("div", "form_body", null, "Really delete event \"" + eventData.description + "\"?");

    //Show
    var isLoading = false;
    showModal({
        "title": "Delete Event",
        "content": content,
        "buttons": [
            {
                "title": "Delete",
                "classname": "modal_window_footer_nav_btn_red",
                "action": (btnDom) => {
                    if (!isLoading) {
                        //Set loading flag so we don't try and do it multiple times
                        isLoading = true;

                        //Set button to be spinner
                        btnDom.classList.add("modal_window_footer_nav_btn_loading");

                        //Send
                        var req = {
                            "id": eventData.id
                        }
                        httpPost("/api/events", req, "DELETE", (result) => {
                            //Close modal
                            closeModal();

                            //Refresh events
                            refreshEvents();
                        }, () => {
                            //Unknown server error - Clear to try again
                            alert("Server error.");
                            btnDom.classList.remove("modal_window_footer_nav_btn_loading");
                            isLoading = false;
                        });
                    }
                }
            },
            {
                "title": "Cancel",
                "classname": "modal_window_footer_nav_btn_float",
                "action": () => {
                    closeModal();
                }
            }
        ]
    });
}

// Creates an event DOM item and returns it from event data.
function createEventItem(eventData) {
    //Create top level
    var root = createDom("div", "evt_item");
    var partDate = createDom("div", "evt_item_date", root);
    var partInfo = createDom("div", "evt_item_info", root);
    var partNav = createDom("div", "evt_item_nav", root);

    //Create date component
    var date = new Date(eventData.time);
    createDom("div", "evt_item_date_sub", partDate, DATE_MONTH_NAMES[date.getMonth()]);
    createDom("div", "evt_item_date_big", partDate, date.getDate());
    createDom("div", "evt_item_date_sub", partDate, createTimeString(date.getHours(), date.getMinutes()));

    //Create string of reciving sites. Do it in a weird way here to keep them in order from the server
    var toSitesList = [];
    for (var i = 0; i < netInfo.sites.length; i++) {
        if (eventData.command.to_sites.includes(netInfo.sites[i].index) || netInfo.sites[i].index == eventData.command.from_site) {
            toSitesList.push(netInfo.sites[i].name);
        }
    }

    //Create info component
    createDom("div", "evt_item_info_title", partInfo, eventData.description);
    var stations = createDom("div", "evt_item_info_stations", partInfo);
    createDom("div", "evt_item_info_stations_from", stations, netInfo.sites[eventData.command.from_site].sources[eventData.command.from_site_source].name);
    var stationsTo = createDom("div", "evt_item_info_stations_to", stations);
    for (var i = 0; i < toSitesList.length; i++) {
        var toSiteStr = toSitesList[i];
        if (i + 1 < toSitesList.length) {
            toSiteStr += ", ";
        }
        createDom("span", null, stationsTo, toSiteStr);
    }

    //Create edit button
    var btnEdit = createDom("div", "evt_item_nav_btn", partNav);
    btnEdit.classList.add("evt_item_nav_btn_edit");
    btnEdit.__event_data = eventData;
    btnEdit.addEventListener("click", function () {
        showEditEventDialog(this.__event_data);
    });

    //Create delete button
    var btnDelete = createDom("div", "evt_item_nav_btn", partNav);
    btnDelete.classList.add("evt_item_nav_btn_delete");
    btnDelete.__event_data = eventData;
    btnDelete.addEventListener("click", function () {
        showDeleteEventDialog(this.__event_data);
    });

    return root;
}

// Refreshes the event part of the page
function refreshEvents() {
    //Get container
    var container = document.getElementById("events");

    //Clear container
    while (container.firstChild != null) {
        container.firstChild.remove();
    }

    //Create loader as placeholder while events load
    var loader = createDom("div", "evt_loader", container);

    //Fetch events
    httpGet("/api/events", (eventData) => {
        //Remove loader
        loader.remove();

        //Add all
        for (var i = 0; i < eventData.events.length; i++) {
            container.appendChild(createEventItem(eventData.events[i]));
        }

        //If there are none, add a banner
        if (eventData.events.length == 0) {
            createDom("div", "evt_banner", container, "There are no upcoming events.");
        }
    }, () => {
        //Remove loader
        loader.remove();

        //Error
        createDom("div", "evt_banner", container, "Error loading events. Try refreshing.");
    });
}

// Shows a dialog asking if expert mode should be enabled
function showExpertModeDialog() {
    //Build dialog
    var content = createDom("div", "form_body", null, "Enable expert mode?\n\nThis could break things. If you don't know what this is, press the cancel button.");

    //Show
    var isLoading = false;
    showModal({
        "title": "Expert Mode",
        "content": content,
        "buttons": [
            {
                "title": "Yes",
                "classname": "modal_window_footer_nav_btn_red",
                "action": (btnDom) => {
                    expertMode = true;
                    closeModal();
                }
            },
            {
                "title": "Cancel",
                "classname": "modal_window_footer_nav_btn_float",
                "action": () => {
                    closeModal();
                }
            }
        ]
    });
}

// Function that sets up after we have recieved the info data.
function postInfoInit() {
    //Get status area
    var statusArea = document.getElementById("status");

    //Create each site's DOM in the status screen
    for (var i = 0; i < netInfo.sites.length; i++) {
        //Create
        var root = createDom("div", "main_status_site", statusArea);
        var title = createDom("div", "main_status_site_label", root, netInfo.sites[i].name);
        var status = createDom("div", "main_status_site_source", root, "Loading..");

        //Add events
        root.__site_index = i;
        root.addEventListener("click", function() {
            showSiteSetup(this.__site_index);
        });

        //Add
        siteStatus[netInfo.sites[i].index] = {
            "root": root,
            "title": title,
            "status": status
        };
    }

    //Refresh parts
    refreshStatus();
    refreshEvents();

    //Remove loader
    document.getElementById("loading").remove();
}

//Init
httpGet("/api/info", (data) => {
    //Set global state
    netInfo = data;

    //Init
    postInfoInit();
}, () => {
    alert("Failed to fetch info. Try refreshing.");
});