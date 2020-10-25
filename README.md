
# async-port-scanner

Async TCP Port Scanner WPF tool written in .NET Core 3.1 SDK
Currently available for only 1-4000 multitasks.

## Installation

Release not available for now.. Open for contribution.

## Usage

Parameters for custom scan:
IP Range: CIDR ip notation or 127.0.0.1-245 type of range notation.
Number of Parallel Tasks: # of threads sharing the ports and ips. Currently max: 4000
Connection Timeout: Waiting time for response in milliseconds. Default: 5000
Number of Active Connections: Max number of connections at arbitrary time of scanning. (Be careful since you're opening tcp socket)


## Contributing
Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.

Please make sure to update tests as appropriate.
