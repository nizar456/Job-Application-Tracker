window.appTokenStore = {
    cookieName: "job-tracker-auth",
    get: function () {
        return localStorage.getItem("job-application-tracker-auth");
    },
    set: function (value) {
        localStorage.setItem("job-application-tracker-auth", value);
        this.setCookie(true);
    },
    remove: function () {
        localStorage.removeItem("job-application-tracker-auth");
        this.setCookie(false);
    },
    setCookie: function (present) {
        if (present) {
            document.cookie = this.cookieName + "=1; Path=/; Max-Age=31536000; SameSite=Lax";
        } else {
            document.cookie = this.cookieName + "=; Path=/; Max-Age=0";
        }
    }
};