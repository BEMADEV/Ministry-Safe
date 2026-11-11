# Ministry-Safe
<div class='alert alert-warning'>All plugins maintained by BEMA are free and fully available for use for anyone using RockRMS. Our goal is to keep versions updated and features added, but these all happen through time that we have available outside of our regular business services. Given these constraints, it may take some time for us to incorporate a feature or update support to the latest version of Rock. </br>
</br>
If you wish to have the timeline moved along for a specific plugin, we can discuss your ministry sponsoring that project. We require a detailed request added to our support link and also an email to ben.chapman@bemaservices.com to communicate the desire to sponsor (our minimum for work order is 4hrs). </div>

## Webhook Authentication

The MinistrySafe settings block can optionally require HTTP Basic Authentication on incoming webhooks. Configure both a username and password to enable it; leave both blank to keep authentication disabled. The password is stored encrypted, and the block lets administrators replace it or clear both credentials without displaying the saved password.

Use HTTPS for the webhook URL. HTTP Basic credentials are Base64-encoded, not encrypted, and must not be sent over an unencrypted connection.
