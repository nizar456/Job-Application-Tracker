window.appTokenStore = {
    get: function () {
        return localStorage.getItem("job-application-tracker-auth");
    },
    set: function (value) {
        localStorage.setItem("job-application-tracker-auth", value);
    },
    remove: function () {
        localStorage.removeItem("job-application-tracker-auth");
    }
};