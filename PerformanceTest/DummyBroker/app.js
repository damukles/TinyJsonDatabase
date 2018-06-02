var mosca = require('mosca');

// var ascoltatore = {
//     //using ascoltatore
//     type: 'mongo',
//     url: 'mongodb://localhost:27017/mqtt',
//     pubsubCollection: 'ascoltatori',
//     mongo: {}
// };

// Cannot start more than one server in one process or the last will win. WHY?
const endpoint = parseInt(process.argv[2]) + 2000;

var sendingHello = false;

var settings = {
    port: endpoint,
    // backend: ascoltatore
};

var server = new mosca.Server(settings);

server.on('clientConnected', function (client) {
    console.log('client connected', client.id);

    sendingHello = true;

    var index = 0;

    setTimeout(function () {
        setInterval(function () {
            if (!sendingHello) return;

            var message = {
                topic: '/loadtest/' + client.id,
                payload: 'Hello ' + client.id + index, // or a Buffer
                qos: 0, // 0, 1, or 2
                retain: false // or true
            };

            index++;

            server.publish(message, function () {
                // console.log('Broker sent a warm hello to ' + client.id)
            });
        }, 5);
    }, 1000);

});

server.on('clientDisconnected', function () {
    sendingHello = false;
});

// fired when a message is received
// server.on('published', function (packet, client) {
// console.log('Published: ', packet.payload);
// });

server.on('ready', setup);

// fired when the mqtt server is ready
function setup() {
    console.log('Mosca server is up and running');
}