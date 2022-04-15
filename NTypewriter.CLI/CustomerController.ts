
module App { 

    export class CustomerController {

        constructor(private $http: ng.IHttpService) { 
        } 
        
        public createCustomer = (customer: CustomerModel) : ng.IHttpPromise<number> => {
            
            return this.$http<number>({
                url: `Customer`, 
                method: "post", 
                data: customer
            });
        };
    }
    
    angular.module("App").service("CustomerService", ["$http", CustomerController]);
}