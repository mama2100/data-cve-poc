package com.memlastic.controller;

import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RestController;

/**
 * A simple controller
 */
@RestController
public class TestController {

    @PostMapping("/upload")
    public String upload(@RequestBody byte[] data) {
        return "Ok";
    }
}
