CacheSplain - A tool for helping with Memcached
=============
![Build Status](https://travis-ci.org/kog/cachesplain.svg?branch=master)

This readme provides a basic set of information about CacheSplain. CacheSplain is MIT licensed. Please see [LICENSE.txt](https://github.com/kog/cachesplain/blob/master/LICENSE.txt) for details.

It helps if you have .NET already installed (Mono, MS CLR), libpcap installed (via WireShark or otherwise) and have some familiarity with the [Memcached binary protocol](https://code.google.com/p/memcached/wiki/BinaryProtocolRevamped).


What is this?
--

CacheSplain is a utility, written in C#, that helps you make sense of what your Memcache clients/servers are doing at a network level. CacheSplain takes either a live network interface or a prior capture via [tcpdump](http://www.tcpdump.org/), [WireShark](https://www.wireshark.org/), or other such utility and analyzes it for Memcached binary protocol traffic. Each TCP packet is inspected and broken down into its individual operations, then reconstituted into lightweight data structures that make the traffic easier to understand. Once the traffic is broken down, it can be stored in something like [MongoDB](http://www.mongodb.org/) ***(TBD)*** or piped into something like [LogStash](http://logstash.net/) ***(TBD)***.

While it's true there are a number of Memcached-related utilities, they all seemed to fall short of what I was looking for:

 * [WireShark](https://www.wireshark.org/) - Has a Memcached protocol analyzer, but is somewhat cumbersome, has trouble with pipelined operations (multiple operations in a single TCP packet). Can be rather slow with larger captures, can be somewhat painful on a Mac (though getting better).
 * [Etsy's McTop](https://github.com/etsy/mctop) - Doesn't support the binary protocol, extremely limited, tends to drop packets under load. Incredibly limited in terms of what it tracks.
 * [Memcache Top](https://code.google.com/p/memcache-top/) - does what it says on the tin... it's Top, which doesn't really provide details as to ***why*** you're doing what you're doing.
 * [MemKeys](https://github.com/bmatheny/memkeys) - didn't quite do what I needed, and since it's written in C++ I thought I'd avoid hacking on it...
 * [The official links](https://code.google.com/p/memcached/wiki/NewLinks) - A lot of these were different types of monitoring or incomplete, or had significant caveats - many are just plain abandoned.

McTop was actually the most useful project (for my needs) I'd found before I started writing CacheSplain, and I'd originally hacked it to support the binary protocol using a combination of the pull requests on the project. This still dropped a lot of packets, so it seemed that it was time to do what all good novices do and write something from scratch...


Why would you write this in C#?
--

So why choose C#? The short answer is that I wasn't particularly happy with the PCAP library support in Java: both of the major contenders I tried had different issues. Ruby was out due to performance (not... that I'm great with it anyway), and I didn't really want to deal with either C or C++.

That's when I remembered that [ShapPcap](http://sourceforge.net/projects/sharppcap/) was supposed to be a pretty decent, fairly well performing library. It's cross platform and works quite well on Mono. SharpPcap supports filtering, it supports PCAP files for both reading and writing, and it's got some great documentation on [CodeProject](http://www.codeproject.com/Articles/12458/SharpPcap-A-Packet-Capture-Framework-for-NET).

I have yet to try this on incredibly large dumps, so it's possible that it has issues scaling to gigs and terabytes of data. So far it's handled my ~500mb dumps just fine. I would highly recommend using tcpdump in your production environment and replaying the files in CacheSplain.


Build Instructions
--

**(TODO: Try and remove WireShark install dependency)**

Before you try and build or run CacheSplain you should install [WireShark](https://www.wireshark.org/) for your platform. This should ensure that you have the appropriate libraries (namely libpcap). This may be addressed at a later time, hopefully working out of the box.

Once you have WireShark installed, you should be able to build the project using an IDE such as Visual Studio or MonoDevelop, or by using something like XBuild on Mono. The build should work out of the box (check the build status top of the page), and you should be able to run the resulting executable. Please note that the App.config is only used for configuring NLog, and all arguments are passed on the command line.

If you are planning on running this on anything but Windows, make sure that you have [Mono](http://www.mono-project.com/) installed. If you can't find a package for your OS/distro, you can always compile your own from source easily enough from their [GitHub repo](https://github.com/mono/mono).


###### Building via XBuild (Mono):
```
nuget restore cachesplain.sln
xbuild cachesplain.sln /target:Rebuild /p:Configuration=release /p:PlatformTarget=x86
```

Don't have NuGet for some reason? Grab the Command-Line Utility from [NuGet.org](http://docs.nuget.org/docs/start-here/installing-nuget). You'll have to prefix it with "mono" (which should hopefully be on your path...).


Cross Platform Information
--

So far I've tested on both Windows 7 and OSX. I've previously installed WireShark on both machines.

The issues found so far are:

* Localhost snooping (lo0) doesn't seem to work on OSX due to PacketDotNet not implementing [null link layer encapsulation](http://wiki.wireshark.org/NullLoopback). Use a PCAP file or sniff a remote machine to get around this.
* The device names on Windows can be incredibly cumbersome to copy/paste.
* Haven't tested on Linux.

As mentioned above, you will need to obtain [Mono](http://www.mono-project.com/) if you wish to run this on an operating system other than Windows. If you are running this on Windows, please make sure you have .NET 4.5 installed.


Example Usages
--

Please note that these examples were written from my Mac, which is why you see the commands prefixed with "mono" - you can omit this if you're not using Mono (IE: Windows).


###### Asking for help:
```
$ mono cachesplain.exe --help
Usage: cachesplain [OPTIONS]
Start listening for packets on a specific port for a given interface.

Options:
-d                         enumerate the network devices available for
                             listening.
-i[=NAME]                  the NAME of the interface to listen on. Will be
                             ignored if an input PCAP file is specified.
-p[=PORT]                  the PORT to listen on.
                             this must be an integer. Defaults to 11211 if
                             not otherwise specified.
-h, --help                 show this message and exit
-f[=VALUE]                 a PCAP file to use instead of a device. If
                             specified, will be used as the input device
                             instead of specified interface.
-x[=VALUE]                 An optional app-level filter expression to filter
                             out packets (IE: opcode, magic, flags etc).
                             Please note this is run across a parsed
                             MemcachedBinaryOperation.
```

It should go without saying that this documentation (README.md) may be out of date, and that you should always refer to the built in help option. If there are any conflicts, please trust whatever asking for help (-h, or --help) tells you.


###### Listening to a particular device, on a particular port:
```
$ mono cachesplain.exe -i=en1 -p=11212
Starting capture... SIGTERM to quit.
```

At this point you'd start reading your out.log. This mode will read until SIGTERM is caught by the process (ex: control+c).


###### Listening to multiple devices/on multiple ports:

*At present there's no way to do this from a single process - you'll need to run multiple instances of CacheSplain to do this. You'll probably want a distinct directory for each, as the logging just goes to out.log for now. This should improve when other output options arrive.*


###### Reading a PCAP file:
```
$ mono cachesplain.exe -f=/Users/kog/dump.pcap -p=11211
Starting capture... SIGTERM to quit.
```

At this point CacheSplain will read through all the packets in your PCAP dump and attempt to pick out the relevant Memcached traffic on the specified port. The port is incredibly vital here because you may have a really dirty cap file, including any amount of random traffic (DNS, HTTP etc.).

This is the recommended use case in a live, functional, production environment.


##### Enumerating network device names
```
$ cachesplain.exe -d
interface: Name: rpcap://\Device\NPF_{GUID-HERE}
FriendlyName: Local Area Connection
GatewayAddress: 192.168.1.1
Description: Network adapter 'Some sort of device name' on local host
Addresses:
Addr:      192.168.1.2
Netmask:   255.255.255.0
Broadaddr: 255.255.255.255

Addresses:
Addr:      HW addr: <MAC ADDRESS>

Flags: 0
...

```

This is mainly useful on Windows since you'll need the interface name - all of it including the "rpcap" portion of "rpcap://\Device\NPF_{GUID-HERE}", where the GUID is whatever you get back from your actual device. This is why the example is being shown from a Windows host. On OSX or Linux you can generally use whatever device name you pick up from `ifconfig`.


Expression Based Filtering
--

CacheSplain incorporates [Solenoid-Expressions](https://github.com/jakesays/Solenoid-Expressions), a fork of [Spring.NET's](http://springframework.net/) implementation of SPEL (Spring Expression Language) in order to allow users to do fine grained, post parsing filtering of operations.

This means that if you know what the [MemcachedBinaryOperation](https://github.com/kog/cachesplain/blob/master/cachesplain/Protocol/MemcachedBinaryOperation.cs) ([class diagram](https://raw.githubusercontent.com/wiki/kog/cachesplain/images/packet-structure.png)) and associated object model look like, you can filter out the noise that you don't care about analyzing. Say you've got a keep-alive at your app level that constantly sends "version" calls you don't care about, you can pass in an expression to ignore these - with something like `"Opcode == OpCode.Version"`.

The filter works by evaluating the expression as a boolean condition, and only allowing operations through that are true. This means that if you have an expression that can't be parsed (IE: it's random typing), it'll be ignored. Likewise, if you write an expression that can't be parsed to a boolean, it won't filter anyting either. The object input to the expression evaluation is the MemcachedBinaryOperation itself.

When writing filters it's helpful to remember that you're mainly doing property accesses as if you're looking at the MemcachedBinaryOperation as your root object. As in the example above, the "this" is implied on the left-hand side when you say "Opcode" - it means more or less `this.OpCode`.


#### Here are some handy examples of things you might try:


###### Looking for things based on a key
```
$ mono cachesplain.exe -i=en1 -p=11211 -x="Key=='SOMEKEY'"
Starting capture... SIGTERM to quit.
```

This will filter out requests that do not match the given key. Unfortunately it may not be as useful as you'd think: the binary protocol requires most operations that take a key for the request not to return the key as part of the response. The exceptions to this rule are the GetK and GetKQ - get key, get key/quiet - operations, which a given client may or may not use.

If you can't trigger your client to send GetK/GetKQ requests, [in the future](https://github.com/kog/cachesplain/issues/1) you may be able to track the opaque/IP/port of the get request to the corresponding response. This may get a little tricky in a high volume, clustered situation.


###### Filtering based on Opcode:
```
$ mono cachesplain.exe -i=en1 -p=11211 -x="Opcode == Opcode.get"
Starting capture... SIGTERM to quit.
```

This will filter out any traffic that is not a get operation (request or response). The end result is that your out.log will contain only get operations - or nothing if there are no get operations.

Read the [Opcode.cs source](https://github.com/kog/cachesplain/blob/master/cachesplain/Protocol/Opcode.cs) for other things you can filter on.


###### Filtering inbound traffic (responses from Memcached):
```
$ mono cachesplain.exe -i=en1 -p=11211 -x="Magic == Magic.received"
Starting capture... SIGTERM to quit.
```

By investigating the "magic" from the header, we can tell what direction the traffic is going in. Requested is from the client to the server, received is the inverse (server -> client). This can be incredibly handy if you're looking at something like traffic on a server or some sort of proxy/network appliance.

Check out the [MemcachedBinaryOperation.cs source](https://github.com/kog/cachesplain/blob/master/cachesplain/Protocol/MemcachedBinaryOperation.cs) for other things you can filter on.


###### Filtering outbound traffic (requests to Memcached):
```
$ mono cachesplain.exe -i=en1 -p=11211 -x="Magic == Magic.requested"
Starting capture... SIGTERM to quit.
```

As you can tell, this is pretty much the inverse of what you see in the previous example...


###### Looking for large operations (large keys, large objects):
```
$ mono cachesplain.exe -i=en1 -p=11211 -x="Header.totalBodyLength > 300"
Starting capture... SIGTERM to quit.
```

In this case I've decided that any operation larger than 300 bytes is "large." Please note that this is at the individual operation level, and not at the *packet* level - packets can have lots of individual, smaller operations (this is called *pipelining*).

It's also worth noting that many Memcached clients will have a compression threshold that tells the client to compress objects over a given threshold, so this value may be especially relevant if you think your client is broken. Remember that compression is a time/size trade off: smaller objects, more CPU time on the clients.


###### Looking for a specific operation (via opaque):
Every operation is given a number that is known as the *opaque* - this value is generated by the client and attached to a given outgoing operation. You can consider this something like a correlation ID. This is called the opaque because it is completely opaque to Memcached: it reads the value from the input event and copies it back into the response event.

An interesting side effect is that you can do something like this:

```
$ mono cachesplain.exe -i=en1 -p=11211 -x="Header.opaque == 80159050"
Starting capture... SIGTERM to quit.
```

You'd probably never need to do this, unless you were doing something like writing your own client.


###### Looking for things with large expiration times
```
$ mono cachesplain.exe -i=en1 -p=11211 -x="Extras != null and Extras.expiration >= 3600"
Starting capture... SIGTERM to quit.
```

Note that because the .NET implementation of SPEL does not support the null-safe operator, we need to do all kinds of annoying null checks ([Solenoid issue #1](https://github.com/jakesays/Solenoid-Expressions/issues/1)).

Looking at expiration values can be handy for a variety of reasons:

 * [Memcached](https://code.google.com/p/memcached/wiki/BinaryProtocolRevamped#Increment,_Decrement) uses an expiration of 0xffffffff to cause increments/decrements to return not found if the key is unknown (as opposed to setting it to a default value).
 * You may not want your objects to live quite so long. You may not intend for them to do so either.
 * Values over 2,592,000 (30 days in seconds) are a Unix timestamp literal to expire at.
 * 0 never expires (though, can still be evicted).


If you want more information on constructing expressions, please see the Solenoid documentation at [Solenoid-Expressions](https://github.com/jakesays/Solenoid-Expressions).

Acknowledgements
--

First and foremost, Dormando  - the guy who maintains [Memcached](http://memcached.org/) - has been incredibly helpful in terms of answering questions and helping troubleshoot issues. Maintaining Memcached is not his day job, and people do tend to be quite impolite, yet he still found time to respond. Much appreciated.

It's also worth noting that the [BinaryHelper](https://github.com/kog/cachesplain/blob/master/cachesplain/Protocol/BinaryHelper.cs) class is more or less lifted wholesale from the [Apache2](http://opensource.org/licenses/Apache-2.0) licensed .NET Memcached client [EnyimMemcached](https://github.com/enyim/EnyimMemcached). I use the code to do some bit twiddling on multi-byte header fields. I have no idea if EnyimMemcached is any good as I've never used it, but the bit twiddling is mighty fine. No sense in reinventing the wheel if someone's already done it better than you will.

Thanks to [JakeSays](https://github.com/jakesays/) for paring down Spring.NET's expression support into [Solenoid-Expressions](https://github.com/jakesays/Solenoid-Expressions). It really cuts down on the size of dependencies... Here's hoping he'll succeed with his next-gen re-write.

Lastly, this markdown was written using [Mou](http://25.io/mou/) and [Atom](https://atom.io/).
